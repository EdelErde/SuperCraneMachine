using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(Collider2D))]
    public class ConveyorBelt : MonoBehaviour
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
        
        private MaterialPropertyBlock _mpb;
        private static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");

        private readonly List<Rigidbody2D> _onBelt = new List<Rigidbody2D>();
        private float _scroll;

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        private Vector2 WorldDirection => transform.TransformDirection(direction.normalized);

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!Affects(other)) return;

            var rb = other.attachedRigidbody;
            if (rb != null && !_onBelt.Contains(rb))
                _onBelt.Add(rb);
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

            Vector2 dir = WorldDirection;
            float beltSpeed = SpeedValue;
            float beltGrip = GripValue;

            foreach (var rb in _onBelt)
            {
                var item = rb.GetComponent<Item>();
                if (item != null && item.IsDragging) continue;

                Vector2 v = rb.linearVelocity;

                float along = Vector2.Dot(v, dir);
                Vector2 across = v - dir * along;

                float newAlong = Mathf.MoveTowards(along, beltSpeed, beltGrip * Time.fixedDeltaTime);
                rb.linearVelocity = across + dir * newAlong;
            }
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

            _scroll += SpeedValue * textureScrollScale * Time.deltaTime;
            _scroll = Mathf.Repeat(_scroll, 1f);

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            beltSprite.GetPropertyBlock(_mpb);
            _mpb.SetVector(MainTexST, new Vector4(1f, 1f, -_scroll, 0f));
            beltSprite.SetPropertyBlock(_mpb);
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