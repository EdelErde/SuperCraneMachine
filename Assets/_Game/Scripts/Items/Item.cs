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

        public int SellValue => type != null ? type.SellValue : 0;
        public float Mass => type != null ? type.Mass : 1f;

        private Rigidbody2D _rb;
        private bool _dragging;
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
            _dragging = true;
            _target = worldPoint;

            _grabOffset = _rb.position - worldPoint;
            _settleT = 0f;
            Strain = 0f;
        }

        public void OnDrag(Vector2 slotPosition) => _target = slotPosition;

        public void OnDragEnd() => _dragging = false;

        public float Strain { get; private set; }

        private void FixedUpdate()
        {
            if (!_dragging) return;

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

        private void OnCollisionEnter2D(Collision2D c)
        {
            OnImpact?.Invoke(this, c.relativeVelocity.magnitude);
        }
    }
}