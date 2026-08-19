using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // The fuel hole. Items dropped here are NOT converted to fuel directly anymore —
    // that used to happen invisibly via ResourceConverter. Now the hole just buffers
    // the item briefly and spits it out the exit side, same eject pattern as
    // SortingMachine. From there the player (or a drone, later) drags it into a
    // FuelFilter, which is what actually produces Fuel items.
    //
    // We POLL overlaps each physics step instead of relying on OnTriggerStay2D: a
    // settled item sleeps, and Unity stops sending Stay callbacks to sleeping bodies,
    // which would let an item dropped in the hole sit there forever. Polling is immune
    // to sleep state. We still skip items the player is actively dragging, so dragging
    // over the hole never yanks an item out of their hand.
    [RequireComponent(typeof(Collider2D))]
    public class FuelHole : MonoBehaviour
    {
        [Header("Points (place in scene)")]
        [Tooltip("Where accepted items are ejected out the other side. Defaults to this transform.")]
        [SerializeField] private Transform exit;

        [Tooltip("Layers that items live on (leave as Everything if unsure).")]
        [SerializeField] private LayerMask itemLayers = ~0;

        [Header("Feel")]
        [Tooltip("Seconds a buffered item waits before being ejected.")]
        [SerializeField] private float processTime = 0.25f;
        [Tooltip("Speed items are ejected out of the exit.")]
        [SerializeField] private float ejectSpeed = 2f;
        [Tooltip("Extra impulse force pushed in the exit direction on release, on top of the eject speed. 0 = velocity only.")]
        [SerializeField] private float ejectForce = 3f;
        [Tooltip("Direction items are launched from the exit, in the hole's LOCAL space. " +
                 "Leave at (0,0) to use the exit transform's own 'right' axis instead.")]
        [SerializeField] private Vector2 ejectDir = new Vector2(0f, -1f);

        // SFX lives in the dedicated SFX/ components (see FuelHoleSfx), which listen to
        // these events rather than the hole owning sound config itself.
        public event System.Action OnIntake;
        public event System.Action OnEject;

        private class Pending
        {
            public Item item;
            public float readyAt;
        }

        private readonly List<Pending> _buffer = new List<Pending>();
        private readonly HashSet<Item> _known = new HashSet<Item>();

        private Collider2D _col;
        private readonly Collider2D[] _overlap = new Collider2D[16];
        private ContactFilter2D _filter;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            if (exit == null) exit = transform;

            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = itemLayers,
                useTriggers = true,
            };
        }

        private void FixedUpdate()
        {
            PurgeDestroyed();

            if (_col != null)
            {
                int count = _col.Overlap(_filter, _overlap);
                for (int i = 0; i < count; i++)
                {
                    var col = _overlap[i];
                    if (col == null) continue;

                    var item = col.GetComponentInParent<Item>();
                    if (item == null || _known.Contains(item)) continue;
                    if (item.IsDragging) continue;

                    Intake(item);
                }
            }

            for (int i = _buffer.Count - 1; i >= 0; i--)
            {
                var p = _buffer[i];
                if (p.item == null) { _buffer.RemoveAt(i); continue; }
                if (Time.time < p.readyAt) continue;

                Eject(p.item);
                _buffer.RemoveAt(i);
            }
        }

        private void Intake(Item item)
        {
            _known.Add(item);
            item.OnDragEnd();

            var rb = item.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;

            SetItemVisible(item, false);

            _buffer.Add(new Pending { item = item, readyAt = Time.time + processTime });
            OnIntake?.Invoke();
        }

        private void Eject(Item item)
        {
            _known.Remove(item);

            var point = exit != null ? exit : transform;
            item.transform.position = point.position;

            SetItemVisible(item, true);

            var rb = item.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                Vector2 dir = EjectDirection(point);
                rb.linearVelocity = dir * ejectSpeed;
                if (ejectForce > 0f)
                    rb.AddForce(dir * ejectForce, ForceMode2D.Impulse);
            }

            OnEject?.Invoke();
        }

        private Vector2 EjectDirection(Transform point)
        {
            if (ejectDir.sqrMagnitude < 0.0001f)
                return point != null ? (Vector2)point.right : Vector2.right;

            return (Vector2)transform.TransformDirection(ejectDir.normalized);
        }

        private static void SetItemVisible(Item item, bool visible)
        {
            if (item == null) return;
            var renderers = item.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = visible;
        }

        private void PurgeDestroyed()
        {
            for (int i = _buffer.Count - 1; i >= 0; i--)
                if (_buffer[i] == null || _buffer[i].item == null)
                    _buffer.RemoveAt(i);

            _known.RemoveWhere(it => it == null);
        }

        private void OnDrawGizmosSelected()
        {
            if (exit == null) return;

            Gizmos.color = Color.yellow;
            Vector3 origin = exit.position;
            Gizmos.DrawWireSphere(origin, 0.12f);

            Vector3 dir = (Vector3)EjectDirection(exit);
            float len = 0.6f;
            Vector3 tip = origin + dir * len;
            Gizmos.DrawLine(origin, tip);
        }
    }
}