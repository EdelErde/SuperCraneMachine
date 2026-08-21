using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Item : MonoBehaviour, IDraggable
    {
        [SerializeReference] public ItemType type = new Egg();

        // For SfxManager — fires on hard-enough collisions (impact sound). Static
        // because SfxManager needs one subscription point covering every item, not
        // per-instance management for potentially hundreds of live items.
        public static event System.Action<Item, float> OnImpact;

        [Header("Drag feel")]
        [SerializeField] private DragConfig config = new DragConfig();

        private float dragForce => config.dragForce;
        private float dragDamping => config.dragDamping;

        [Header("Drone carry feel")]
        [Tooltip("How firmly the item is pulled up under a carrying drone. Higher = tracks " +
                 "the drone more tightly; lower = hangs looser / lags more behind it.")]
        [SerializeField] private float droneCarryForce = 55f;
        [Tooltip("Damping while a drone carries the item. Higher = less swing/overshoot.")]
        [SerializeField] private float droneCarryDamping = 7f;
        [Tooltip("How far (world units) the item can trail behind the drone's carry point " +
                 "before it counts as slipped and the drone loses it. This is the drone's " +
                 "own leash — the hand's slip rule is ignored during drone carry so a heavy " +
                 "item can't silently drop mid-flight.")]
        [SerializeField] private float droneCarryLeash = 2.4f;

        public int SellValue => type != null ? type.SellValue : 0;
        public float Mass => type != null ? type.Mass : 1f;

        private Rigidbody2D _rb;
        private bool _dragging;
        private bool _droneCarry;      // true while a drone is hauling this (vs the hand)
        private Vector2 _target;
        private Vector2 _grabOffset;
        private float _settleT;

        [Tooltip("Seconds for the item to ease from where it was grabbed into its slot.")]
        [SerializeField] private float settleTime = 0.25f;

        public Transform Transform => transform;
        public bool CanDrag => Time.time >= _regrabTime;
        public bool IsDragging => _dragging;

        [Tooltip("Seconds before a slipped item can be grabbed again.")]
        [SerializeField] private float regrabCooldown = 0.4f;

        private float _regrabTime;

        private static float HandStrength =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.HandStrength) : 5f;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        private void Start()
        {
            if (type != null && ServiceLocator.StatService != null)
                _rb.mass = type.Mass;
        }

        public void OnDragBegin(Vector2 worldPoint)
        {
            // A fresh drag (hand OR drone) always takes over as a HAND-style drag. If a drone
            // was carrying and the player grabs it, this is exactly the hand-over: drone mode
            // clears, the hand's normal drag physics take over, and the drone will notice the
            // item drifting away from its carry point and let go. That's the steal.
            _droneCarry = false;
            _dragging = true;
            _target = worldPoint;

            _grabOffset = _rb.position - worldPoint;
            _settleT = 0f;
            Strain = 0f;
        }

        // Drone-specific pickup. Same "begin a drag at the current position" contract as
        // OnDragBegin, but flags this as a drone carry so FixedUpdate uses the firmer,
        // no-silent-slip physics below. Called by Drone.Grip.
        public void BeginDroneCarry(Vector2 worldPoint)
        {
            _droneCarry = true;
            _dragging = true;
            _target = worldPoint;
            _grabOffset = _rb.position - worldPoint;
            _settleT = 0f;
            Strain = 0f;
        }

        public void OnDrag(Vector2 slotPosition) => _target = slotPosition;

        public void OnDragEnd()
        {
            _dragging = false;
            _droneCarry = false;
        }

        // How far the item currently trails its carry target — the drone reads this to decide
        // whether it still holds the item or it's been pulled away / slipped.
        public float DragError =>
            _dragging ? (_target - (Vector2)_rb.position).magnitude : float.MaxValue;

        public float Strain { get; private set; }

        private void FixedUpdate()
        {
            if (!_dragging) return;

            if (_droneCarry) { DroneCarryStep(); return; }

            _settleT = Mathf.Min(1f, _settleT + Time.fixedDeltaTime / Mathf.Max(0.01f, settleTime));
            Vector2 offset = Vector2.Lerp(_grabOffset, Vector2.zero, _settleT);

            Vector2 desired = _target + offset;

            float distance = (desired - _rb.position).magnitude;

            float maxDistance = HandStrength / Mathf.Max(0.01f, Mass);

            float instantStrain = Mathf.Clamp01(distance / Mathf.Max(0.01f, maxDistance));
            Strain = Mathf.Lerp(Strain, instantStrain, 0.35f);

            if (distance > maxDistance)
            {
                _dragging = false;
                Strain = 0f;
                _regrabTime = Time.time + regrabCooldown;
                return;
            }

            Vector2 toTarget = desired - _rb.position;
            Vector2 force = toTarget * dragForce - _rb.linearVelocity * dragDamping;
            _rb.AddForce(force);
        }

        // Physics while a DRONE is carrying. Differs from the hand drag in two ways that
        // matter for the "it just floats and doesn't follow" bug:
        //   1. A firmer spring (droneCarryForce) mass-compensated so even a heavier item
        //      actually keeps up with the moving carry point instead of trailing loosely.
        //   2. The hand's silent "distance > HandStrength/Mass => drop myself" slip rule is
        //      NOT applied here. The only way out of a drone carry is the drone letting go
        //      (via OnDragEnd) or the player grabbing it (OnDragBegin clears _droneCarry).
        // We still expose DragError so the drone can decide it's been pulled too far and
        // release — but the ITEM never unilaterally drops mid-flight anymore.
        private void DroneCarryStep()
        {
            _settleT = Mathf.Min(1f, _settleT + Time.fixedDeltaTime / Mathf.Max(0.01f, settleTime));
            Vector2 offset = Vector2.Lerp(_grabOffset, Vector2.zero, _settleT);
            Vector2 desired = _target + offset;

            Vector2 toTarget = desired - _rb.position;
            float distance = toTarget.magnitude;

            // Mass-compensate so a heavy item is pulled just as tightly as a light one (the
            // drone provides the lift). Without this, Mass makes the item lag and "float."
            float massComp = Mathf.Max(0.2f, Mass);
            Vector2 force = toTarget * (droneCarryForce * massComp) - _rb.linearVelocity * (droneCarryDamping * massComp);
            _rb.AddForce(force);

            // Strain purely cosmetic here: how close we are to the drone's leash.
            Strain = Mathf.Lerp(Strain, Mathf.Clamp01(distance / Mathf.Max(0.01f, droneCarryLeash)), 0.35f);
        }

        // The drone's leash length (world units) — how far the item may trail before the
        // drone should consider it slipped/stolen. Exposed so the drone and item agree.
        public float DroneCarryLeash => droneCarryLeash;

        private void OnCollisionEnter2D(Collision2D c)
        {
            OnImpact?.Invoke(this, c.relativeVelocity.magnitude);
        }
    }
}