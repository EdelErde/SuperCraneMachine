using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Item : MonoBehaviour, IDraggable
    {
        [SerializeReference] public ItemType type = new RubberDuck();

        [Header("Drag feel")]
        [Tooltip("How strongly the item is pulled toward the pointer.")]
        [SerializeField] private float dragForce = 60f;
        [Tooltip("Higher = less overshoot/wobble while dragging.")]
        [SerializeField] private float dragDamping = 8f;

        public int SellValue => type != null ? type.SellValue : 0;
        public float Mass => type != null ? type.Mass : 1f;

        private Rigidbody2D _rb;
        private bool _dragging;
        private Vector2 _target;

        public Transform Transform => transform;
        public bool CanDrag => true;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        private void Start()
        {
            if (type != null && ServiceLocator.StatService != null)
                _rb.mass = type.Mass;
        }

        public void OnDragBegin() => _dragging = true;

        public void OnDrag(Vector2 worldPoint) => _target = worldPoint;

        public void OnDragEnd() => _dragging = false;

        private void FixedUpdate()
        {
            if (!_dragging) return;

            Vector2 toTarget = _target - _rb.position;
            Vector2 force = toTarget * dragForce - _rb.linearVelocity * dragDamping;
            _rb.AddForce(force);
        }
    }
}