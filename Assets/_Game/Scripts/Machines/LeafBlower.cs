using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // A static object that continuously blows any items inside its zone in a set
    // local direction. Consumes fuel over time while it is actually blowing something.
    // No fuel -> it stops (items just fall / sit).
    //
    // Follows the ConveyorBelt pattern: track rigidbodies in a trigger, act in FixedUpdate.
    [RequireComponent(typeof(Collider2D))]
    public class LeafBlower : MonoBehaviour, IFuelConsumer, IToggleableMachine
    {
        [Header("Blow")]
        [Tooltip("Direction the blower pushes, in local space.")]
        [SerializeField] private Vector2 direction = Vector2.right;
        [Tooltip("Extra base force multiplier on top of the BlowPower stat.")]
        [SerializeField] private float forceScale = 1f;
        [Tooltip("Optional falloff: items further along the zone get less push (0 = uniform).")]
        [SerializeField] private float falloff = 0f;

        [Header("Fuel")]
        [Tooltip("Base fuel units drained per second while blowing (before efficiency).")]
        [SerializeField] private float fuelPerSecond = 0.5f;
        [Tooltip("Name shown for this blower in the production/fuel view.")]
        [SerializeField] private string fuelLabel = "Leaf Blower";

        [Header("Power")]
        [Tooltip("Whether the blower starts switched on.")]
        [SerializeField] private bool startEnabled = true;

        [Header("Filter")]
        [SerializeField] private LayerMask affectedLayers = ~0;

        [Header("SFX (optional)")]
        [Tooltip("Looping/one-shot source played while actively blowing.")]
        [SerializeField] private SfxSource blowSfx;
        [Tooltip("Minimum seconds between blow SFX plays.")]
        [SerializeField] private float sfxInterval = 0.4f;

        private readonly List<Rigidbody2D> _inZone = new List<Rigidbody2D>();
        private float _nextSfx;

        private Vector2 WorldDirection => transform.TransformDirection(direction.normalized);

        private float BlowPower =>
            ServiceLocator.StatService != null
                ? ServiceLocator.StatService.GameValue(GameStat.BlowPower)
                : 6f;

        private float FuelEfficiency =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0.01f, ServiceLocator.StatService.GameValue(GameStat.BlowFuelEfficiency))
                : 1f;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!Affects(other)) return;
            var rb = other.attachedRigidbody;
            if (rb != null && !_inZone.Contains(rb))
                _inZone.Add(rb);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            if (rb != null) _inZone.Remove(rb);
        }

        private bool Affects(Collider2D other)
        {
            if ((affectedLayers.value & (1 << other.gameObject.layer)) == 0) return false;
            return other.GetComponentInParent<Item>() != null;
        }

        private void FixedUpdate()
        {
            _inZone.RemoveAll(rb => rb == null);

            bool blowing = false;

            if (_enabled && _inZone.Count > 0 && ServiceLocator.FuelService != null)
            {
                // Fuel drain scales with how many items we're actually pushing this step.
                float wanted = fuelPerSecond / FuelEfficiency * Time.fixedDeltaTime;
                float spent = ServiceLocator.FuelService.SpendUpTo(wanted);

                if (spent > 0f)
                {
                    blowing = true;
                    CurrentFuelDraw = fuelPerSecond / FuelEfficiency;

                    // If we could only afford part of the drain, scale the push proportionally.
                    float budget = spent / Mathf.Max(0.0001f, wanted); // 0..1
                    float power = BlowPower * forceScale * budget;

                    Vector2 dir = WorldDirection;
                    foreach (var rb in _inZone)
                    {
                        if (rb == null) continue; // destroyed this frame

                        var item = rb.GetComponent<Item>();
                        if (item != null && item.IsDragging) continue;

                        float f = power;
                        if (falloff > 0f)
                        {
                            float along = Vector2.Dot((Vector2)rb.position - (Vector2)transform.position, dir);
                            f *= Mathf.Clamp01(1f - along * falloff);
                        }

                        rb.AddForce(dir * f);
                    }

                    PlaySfx();
                }
            }

            IsBlowing = blowing;
            if (!blowing) CurrentFuelDraw = 0f;
        }

        // True while the blower is actively pushing items (has items in zone + fuel).
        public bool IsBlowing { get; private set; }

        // Fuel units per second currently drawn (0 when idle). For the production view.
        public float CurrentFuelDraw { get; private set; }

        // Blow direction in world space, for the particle emitter to align to.
        public Vector2 BlowWorldDirection => WorldDirection;

        // ---- IFuelConsumer ----
        public string FuelLabel => fuelLabel;

        // ---- IToggleableMachine ----
        private bool _enabled = true;
        private bool _initialised;

        public string ToggleLabel => fuelLabel;

        public event System.Action<bool> OnToggled;

        public bool MachineEnabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (!_enabled)
                {
                    IsBlowing = false;
                    CurrentFuelDraw = 0f;
                }
                OnToggled?.Invoke(_enabled);
            }
        }

        private void Awake()
        {
            _enabled = startEnabled;
            _initialised = true;
        }

        private void OnEnable()
        {
            // Honour the serialized start state the first time (Awake may not have run
            // yet on the very first enable in edit-play transitions).
            if (!_initialised) { _enabled = startEnabled; _initialised = true; }

            if (ServiceLocator.FuelConsumers != null)
                ServiceLocator.FuelConsumers.Register(this);
        }

        private void OnDisable()
        {
            if (ServiceLocator.FuelConsumers != null)
                ServiceLocator.FuelConsumers.Unregister(this);
            IsBlowing = false;
            CurrentFuelDraw = 0f;
        }

        private void PlaySfx()
        {
            if (blowSfx == null) return;
            if (Time.time < _nextSfx) return;
            _nextSfx = Time.time + Mathf.Max(0f, sfxInterval);
            blowSfx.Play();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 c = transform.position;
            Vector3 d = (Vector3)WorldDirection;
            Gizmos.DrawLine(c, c + d);
            Gizmos.DrawSphere(c + d, 0.06f);
        }
    }
}