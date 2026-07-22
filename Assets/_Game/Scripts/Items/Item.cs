using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Item : MonoBehaviour, IDraggable
    {
        [SerializeReference] public ItemType type = new RubberDuck();

        [Header("Drag feel")]
        [SerializeField] private DragConfig config = new DragConfig();

        private float dragForce => config.dragForce;
        private float dragDamping => config.dragDamping;

        public int SellValue => type != null ? type.SellValue : 0;
        public float Mass => type != null ? type.Mass : 1f;

        private Rigidbody2D _rb;
        private bool _dragging;
        private Vector2 _target;

        public Transform Transform => transform;
        public bool CanDrag => true;
        public bool IsDragging => _dragging;

        private static float HandStrength =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.HandStrength) : 5f;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        private void Start()
        {
            if (type != null && ServiceLocator.StatService != null)
                _rb.mass = type.Mass;
        }

        public void OnDragBegin()
        {
            _dragging = true;
        }

        public void OnDrag(Vector2 worldPoint) => _target = worldPoint;

        public void OnDragEnd() => _dragging = false;

        public float Strain { get; private set; }

        private void FixedUpdate()
        {
            if (!_dragging) return;

            float distance = (_target - _rb.position).magnitude;

            float maxDistance = HandStrength / Mathf.Max(0.01f, Mass);

            float instantStrain = Mathf.Clamp01(distance / Mathf.Max(0.01f, maxDistance));
            Strain = Mathf.Lerp(Strain, instantStrain, 0.35f);

            if (distance > maxDistance)
            {
                _dragging = false;
                Strain = 0f;
                return;
            }

            Vector2 toTarget = _target - _rb.position;
            Vector2 force = toTarget * dragForce - _rb.linearVelocity * dragDamping;
            _rb.AddForce(force);
        }
    }
}