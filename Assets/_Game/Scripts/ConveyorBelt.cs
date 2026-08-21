using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(Collider2D))]
    public class ConveyorBelt : MonoBehaviour, IFuelConsumer, IToggleableMachine
    {
        [Header("Motion")]
        [Tooltip("Belt speed in units per second. Negative runs it the other way.")]
        [SerializeField] private float speed = 2f;
        [Tooltip("Direction the belt pushes, in local space.")]
        [SerializeField] private Vector2 direction = Vector2.right;
        [Tooltip("How quickly an item is brought up to belt speed.")]
        [SerializeField] private float grip = 12f;

        [Header("Filter")]
        [SerializeField] private LayerMask affectedLayers = ~0;
        [Tooltip("Only move objects that have an Item component.")]
        [SerializeField] private bool itemsOnly = true;

        [Header("Visuals")]
        [Tooltip("Optional. Scrolls the sprite's texture to look like it's moving.")]
        [SerializeField] private SpriteRenderer beltSprite;
        [SerializeField] private float textureScrollScale = 1f;
        [Tooltip("Optional rumble that plays while the belt is carrying items.")]
        [SerializeField] private MachineRumble rumble;

        [Header("Fuel")]
        // Base fuel drained per second while items are on the belt. Code-defined constant,
        // NOT a [SerializeField]: a stale scene value used to silently override the script and
        // break fuel balance. Rebalance by editing this line. (0 = free.)
        private const float fuelPerSecond = 0.25f;
        [Tooltip("If true and out of fuel, the belt stops moving items.")]
        [SerializeField] private bool requiresFuel = true;
        [Tooltip("Name shown for this belt in the production/fuel view.")]
        [SerializeField] private string fuelLabel = "Conveyor";

        [Header("Power")]
        [Tooltip("Whether the belt starts switched on.")]
        [SerializeField] private bool startEnabled = true;
        
        private readonly List<Rigidbody2D> _onBelt = new List<Rigidbody2D>();
        private float _scroll;
        private bool _running;

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        private Vector2 WorldDirection => transform.TransformDirection(direction.normalized);

        // ---- IFuelConsumer ----
        public string FuelLabel => fuelLabel;
        public float CurrentFuelDraw { get; private set; }

        // ---- IToggleableMachine ----
        private bool _enabled = true;
        private bool _initialised;

        public string ToggleLabel => fuelLabel;

        public event System.Action<bool> OnToggled;

        // For SfxManager — fires when an item enters the belt (was previously a
        // dedicated ConveyorSfx component's OnTriggerEnter2D; centralized now).
        public event System.Action OnItemEntered;

        public bool MachineEnabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (!_enabled)
                {
                    _running = false;
                    CurrentFuelDraw = 0f;
                    UpdateRumble();
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
            if (!_initialised) { _enabled = startEnabled; _initialised = true; }

            if (ServiceLocator.FuelConsumers != null)
                ServiceLocator.FuelConsumers.Register(this);
        }

        private void OnDisable()
        {
            if (ServiceLocator.FuelConsumers != null)
                ServiceLocator.FuelConsumers.Unregister(this);
            CurrentFuelDraw = 0f;
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!Affects(other)) return;

            var rb = other.attachedRigidbody;
            if (rb != null && !_onBelt.Contains(rb))
            {
                _onBelt.Add(rb);
                OnItemEntered?.Invoke();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            if (rb != null) _onBelt.Remove(rb);
        }

        private bool Affects(Collider2D other)
        {
            if ((affectedLayers.value & (1 << other.gameObject.layer)) == 0) return false;
            if (itemsOnly && other.GetComponentInParent<Item>() == null) return false;
            return true;
        }

        private void FixedUpdate()
        {
            _onBelt.RemoveAll(rb => rb == null);

            // Belt only works (and only burns fuel) while it's actually carrying
            // something AND it's switched on.
            bool carrying = _enabled && _onBelt.Count > 0;
            bool powered = true;

            if (carrying && requiresFuel && fuelPerSecond > 0f && ServiceLocator.FuelService != null)
            {
                float wanted = fuelPerSecond * Time.fixedDeltaTime;
                float spent = ServiceLocator.FuelService.SpendUpTo(wanted);
                powered = spent > 0f;
                CurrentFuelDraw = powered ? fuelPerSecond : 0f;
            }
            else
            {
                CurrentFuelDraw = 0f;
            }

            _running = carrying && powered;
            UpdateRumble();

            if (!_running) return;

            Vector2 dir = WorldDirection;
            float beltSpeed = SpeedValue;
            float beltGrip = GripValue;

            foreach (var rb in _onBelt)
            {
                if (rb == null) continue; // destroyed this frame

                var item = rb.GetComponent<Item>();
                if (item != null && item.IsDragging) continue;

                Vector2 v = rb.linearVelocity;

                float along = Vector2.Dot(v, dir);
                Vector2 across = v - dir * along;

                float newAlong = Mathf.MoveTowards(along, beltSpeed, beltGrip * Time.fixedDeltaTime);
                rb.linearVelocity = across + dir * newAlong;
            }
        }

        private void UpdateRumble()
        {
            if (rumble != null) rumble.SetActive(_running);
        }

        private float SpeedValue =>
            ServiceLocator.StatService != null
                ? Mathf.Sign(speed == 0 ? 1f : speed) * ServiceLocator.StatService.GameValue(GameStat.ConveyorSpeed)
                : speed;

        private float GripValue =>
            ServiceLocator.StatService != null
                ? ServiceLocator.StatService.GameValue(GameStat.ConveyorGrip)
                : grip;

        private void Update()
        {
            if (beltSprite == null) return;

            // Only advance the scroll while the belt is actually running.
            if (_running)
            {
                _scroll += SpeedValue * textureScrollScale * Time.deltaTime;
                _scroll = Mathf.Repeat(_scroll, 1f);
            }

            ApplyScroll();
        }

        private Material _beltMat;
        private void ApplyScroll()
        {
            if (_beltMat == null)
            {
                _beltMat = beltSprite.material;
                if (_beltMat == null) return;
            }

            Vector2 offset = (Vector2)WorldDirection * -_scroll;
            _beltMat.mainTextureOffset = offset;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 c = transform.position;
            Vector3 d = (Vector3)WorldDirection * Mathf.Sign(speed);
            Gizmos.DrawLine(c, c + d);
            Gizmos.DrawSphere(c + d, 0.06f);
        }
    }
}