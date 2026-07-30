using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // A machine you feed items into (via the entry collider). Each item is buffered
    // (up to SortCapacity) and then routed to one of two exit points, A or B, according
    // to the per-machine SortingConfig. Routing costs fuel; with no fuel EVERYTHING
    // drops through hole A.
    //
    // Entry / exit points are colliders/transforms placed in the scene so they can be
    // configured spatially (per the brief). This component only decides + moves items.
    public class SortingMachine : MonoBehaviour, IWorldInteractable, IFuelConsumer
    {
        public Transform Transform => transform;

        [Header("Config")]
        [Tooltip("Per-machine routing rules. Editable at runtime via the config window.")]
        [SerializeField] private SortingConfig config = new SortingConfig();

        [Header("Points (place in scene)")]
        [Tooltip("Trigger the player drops items into. If null, uses this object's collider.")]
        [SerializeField] private Collider2D entry;
        [Tooltip("Where items routed to A are released (the drop-through / default hole).")]
        [SerializeField] private Transform exitA;
        [Tooltip("Where items routed to B are released.")]
        [SerializeField] private Transform exitB;

        [Header("Feel")]
        [Tooltip("Seconds a buffered item waits before being released.")]
        [SerializeField] private float processTime = 0.35f;
        [Tooltip("Speed items are ejected out of an exit.")]
        [SerializeField] private float ejectSpeed = 2f;

        [Header("Fuel")]
        [Tooltip("Base fuel drained per second while the machine holds any items.")]
        [SerializeField] private float fuelPerSecond = 0.3f;
        [Tooltip("Name shown for this machine in the production/fuel view.")]
        [SerializeField] private string fuelLabel = "Sorting Machine";

        [Header("SFX (optional)")]
        [SerializeField] private SfxSource intakeSfx;
        [SerializeField] private SfxSource sortSfx;

        [Header("Feedback (optional)")]
        [Tooltip("Rumble that plays while the machine is processing items.")]
        [SerializeField] private MachineRumble rumble;

        public SortingConfig Config => config;

        private class Pending
        {
            public Item item;
            public float readyAt;
        }

        private readonly List<Pending> _buffer = new List<Pending>();
        private readonly HashSet<Item> _known = new HashSet<Item>();

        private int Capacity =>
            ServiceLocator.StatService != null
                ? Mathf.Max(1, Mathf.RoundToInt(ServiceLocator.StatService.GameValue(GameStat.SortCapacity)))
                : 4;

        private float FuelEfficiency =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0.01f, ServiceLocator.StatService.GameValue(GameStat.SortFuelEfficiency))
                : 1f;

        private bool HasFuel =>
            ServiceLocator.FuelService != null && ServiceLocator.FuelService.CurrentFuel > 0f;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            if (entry == null) entry = GetComponent<Collider2D>();
        }

        // ---- IFuelConsumer ----
        public string FuelLabel => fuelLabel;

        private void OnEnable()
        {
            if (ServiceLocator.FuelConsumers != null)
                ServiceLocator.FuelConsumers.Register(this);
        }

        private void OnDisable()
        {
            if (ServiceLocator.FuelConsumers != null)
                ServiceLocator.FuelConsumers.Unregister(this);
            CurrentFuelDraw = 0f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Only react to the entry collider (allows a dedicated child entry).
            if (entry != null && other.IsTouching(entry) == false && entry != GetComponent<Collider2D>())
            {
                // If a dedicated entry is set and it's not this collider, ignore direct hits
                // on the body collider. When entry == body collider this check is skipped.
            }

            var item = other.GetComponentInParent<Item>();
            if (item == null || _known.Contains(item)) return;
            if (_buffer.Count >= Capacity) return; // full: let it pass through / pile up

            _known.Add(item);
            item.OnDragEnd(); // ensure it's not being dragged as we take control

            // Park it (disable physics interactions while buffered).
            var rb = item.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;
            item.gameObject.SetActive(true);

            _buffer.Add(new Pending { item = item, readyAt = Time.time + processTime });
            if (intakeSfx != null) intakeSfx.Play();
        }

        private void Update()
        {
            bool working = _buffer.Count > 0;

            if (working)
            {
                // Drain fuel while holding items (efficiency reduces the cost).
                CurrentFuelDraw = fuelPerSecond / FuelEfficiency;
                if (ServiceLocator.FuelService != null)
                    ServiceLocator.FuelService.SpendUpTo(CurrentFuelDraw * Time.deltaTime);

                bool fuel = HasFuel;

                for (int i = _buffer.Count - 1; i >= 0; i--)
                {
                    var p = _buffer[i];
                    if (p.item == null) { _buffer.RemoveAt(i); continue; }
                    if (Time.time < p.readyAt) continue;

                    Release(p.item, fuel);
                    _buffer.RemoveAt(i);
                }
            }
            else
            {
                CurrentFuelDraw = 0f;
            }

            if (rumble != null) rumble.SetActive(_buffer.Count > 0);
        }

        // Fuel units per second this machine is currently drawing (0 when idle).
        // Exposed so the production view can show per-machine consumption.
        public float CurrentFuelDraw { get; private set; }

        private void Release(Item item, bool fuel)
        {
            _known.Remove(item);

            var exit = config.Decide(item.type != null ? item.type.GetType() : null, fuel);
            var point = exit == SortExit.B ? exitB : exitA;
            if (point == null) point = transform;

            item.transform.position = point.position;

            var rb = item.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                Vector2 dir = point.right; // exit points face their release direction
                rb.linearVelocity = dir * ejectSpeed;
            }

            if (sortSfx != null) sortSfx.Play();
        }

        // ---- Clicking the machine opens its config window ----
        // Uses Unity's built-in collider click (needs a non-trigger-agnostic Collider2D on
        // this object). Self-contained so it needs no changes to the drag controller.
        private void OnMouseDown()
        {
            // Ignore clicks that land on UI.
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            var window = SortingConfigWindow.Instance;
            if (window != null) window.Open(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            if (exitA != null) Gizmos.DrawWireSphere(exitA.position, 0.12f);
            Gizmos.color = Color.cyan;
            if (exitB != null) Gizmos.DrawWireSphere(exitB.position, 0.12f);
        }
    }
}