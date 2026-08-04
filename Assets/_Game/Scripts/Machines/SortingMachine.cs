using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class SortingMachine : MonoBehaviour, IWorldInteractable, IFuelConsumer, IToggleableMachine
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
        [Tooltip("Extra impulse force pushed in the exit direction on release, on top of the eject speed. 0 = velocity only.")]
        [SerializeField] private float ejectForce = 3f;

        [Header("Eject direction")]
        [Tooltip("Direction items are launched from exit A, in the machine's LOCAL space. " +
                 "(1,0)=right, (-1,0)=left, (0,1)=up, (0,-1)=down, or any diagonal. " +
                 "Leave at (0,0) to use exit A's own 'right' axis instead.")]
        [SerializeField] private Vector2 ejectDirA = new Vector2(0f, -1f);
        [Tooltip("Direction items are launched from exit B, in the machine's LOCAL space. " +
                 "Leave at (0,0) to use exit B's own 'right' axis instead.")]
        [SerializeField] private Vector2 ejectDirB = new Vector2(0f, -1f);

        [Header("Fuel")]
        [Tooltip("Base fuel drained per second while the machine holds any items.")]
        [SerializeField] private float fuelPerSecond = 0.3f;
        [Tooltip("Name shown for this machine in the production/fuel view.")]
        [SerializeField] private string fuelLabel = "Sorting Machine";

        [Header("Power")]
        [Tooltip("Whether the machine starts switched on.")]
        [SerializeField] private bool startEnabled = true;

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
            _enabled = startEnabled;
        }

        // ---- IFuelConsumer ----
        public string FuelLabel => fuelLabel;

        // ---- IToggleableMachine ----
        private bool _enabled = true;

        public string ToggleLabel => fuelLabel;

        public event System.Action<bool> OnToggled;

        public bool MachineEnabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (!_enabled) CurrentFuelDraw = 0f;
                OnToggled?.Invoke(_enabled);
            }
        }

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

            if (entry != null && other.IsTouching(entry) == false && entry != GetComponent<Collider2D>())
            {
                // If a dedicated entry is set and it's not this collider, ignore direct hits
                // on the body collider. When entry == body collider this check is skipped.
            }

            if (!_enabled) return;

            var item = other.GetComponentInParent<Item>();
            if (item == null || _known.Contains(item)) return;
            if (_buffer.Count >= Capacity) return;

            _known.Add(item);
            item.OnDragEnd();

            var rb = item.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;

            SetItemVisible(item, false);

            _buffer.Add(new Pending { item = item, readyAt = Time.time + processTime });
            if (intakeSfx != null) intakeSfx.Play();
        }

        private void Update()
        {
            PurgeDestroyed();

            if (!_enabled)
            {
                if (_buffer.Count > 0)
                {
                    for (int i = _buffer.Count - 1; i >= 0; i--)
                    {
                        var p = _buffer[i];
                        if (p.item != null) Release(p.item, fuel: false);
                        _buffer.RemoveAt(i);
                    }
                }
                CurrentFuelDraw = 0f;
                if (rumble != null) rumble.SetActive(false);
                return;
            }

            bool working = _buffer.Count > 0;

            if (working)
            {
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

        private void PurgeDestroyed()
        {
            for (int i = _buffer.Count - 1; i >= 0; i--)
                if (_buffer[i] == null || _buffer[i].item == null)
                    _buffer.RemoveAt(i);

            _known.RemoveWhere(it => it == null);
        }

        public float CurrentFuelDraw { get; private set; }

        private void Release(Item item, bool fuel)
        {
            _known.Remove(item);

            var exit = config.Decide(item.type != null ? item.type.GetType() : null, fuel);
            var point = exit == SortExit.B ? exitB : exitA;
            if (point == null) point = transform;

            item.transform.position = point.position;

            SetItemVisible(item, true);

            var rb = item.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                Vector2 dir = EjectDirection(exit, point);
                rb.linearVelocity = dir * ejectSpeed;
                if (ejectForce > 0f)
                    rb.AddForce(dir * ejectForce, ForceMode2D.Impulse);
            }

            if (sortSfx != null) sortSfx.Play();
        }

        private Vector2 EjectDirection(SortExit exit, Transform point)
        {
            Vector2 local = exit == SortExit.B ? ejectDirB : ejectDirA;

            if (local.sqrMagnitude < 0.0001f)
                return point != null ? (Vector2)point.right : Vector2.right;

            return (Vector2)transform.TransformDirection(local.normalized);
        }

        private static void SetItemVisible(Item item, bool visible)
        {
            if (item == null) return;
            var renderers = item.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = visible;
        }

        private void OnDrawGizmosSelected()
        {
            DrawExitGizmo(exitA, SortExit.A, Color.green);
            DrawExitGizmo(exitB, SortExit.B, Color.cyan);
        }

        private void DrawExitGizmo(Transform point, SortExit exit, Color color)
        {
            if (point == null) return;

            Gizmos.color = color;
            Vector3 origin = point.position;
            Gizmos.DrawWireSphere(origin, 0.12f);

            Vector3 dir = (Vector3)EjectDirection(exit, point);
            float len = 0.6f;
            Vector3 tip = origin + dir * len;
            Gizmos.DrawLine(origin, tip);

            // Simple 2D arrowhead.
            Vector3 back = dir * -0.15f;
            Vector3 side = new Vector3(-dir.y, dir.x, 0f) * 0.1f;
            Gizmos.DrawLine(tip, tip + back + side);
            Gizmos.DrawLine(tip, tip + back - side);
        }
    }
}