using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Put this on any object with a Collider2D. If a rigidbody ends up overlapping this
    // collider and stays overlapping (e.g. a belt or machine was spawned on top of an
    // item that was already lying there), this gently lifts it straight up and out the
    // top of the collider's bounds until it's clear.
    //
    // Works with both solid and trigger colliders. It only acts on objects that have
    // been stuck for longer than 'graceSeconds', so items passing through normally
    // aren't disturbed.
    [RequireComponent(typeof(Collider2D))]
    public class StuckEjector : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Seconds an object must stay overlapping before it's treated as stuck.")]
        [SerializeField] private float graceSeconds = 0.4f;
        [Tooltip("Only eject objects that have an Item component (ignore scenery/players).")]
        [SerializeField] private bool itemsOnly = true;
        [Tooltip("Which layers can be ejected.")]
        [SerializeField] private LayerMask ejectLayers = ~0;

        [Header("Eject")]
        [Tooltip("How fast the object is pushed upward while stuck (units/sec).")]
        [SerializeField] private float ejectSpeed = 4f;
        [Tooltip("Extra clearance above the top edge before it's considered free.")]
        [SerializeField] private float clearance = 0.1f;
        [Tooltip("Zero the object's downward velocity while ejecting so it doesn't fight the lift.")]
        [SerializeField] private bool cancelDownwardVelocity = true;

        [Header("Scan")]
        [Tooltip("How often (seconds) to re-scan for stuck objects. 0 = every physics step.")]
        [SerializeField] private float scanInterval = 0.2f;

        private Collider2D _col;
        private ContactFilter2D _filter;
        private readonly Collider2D[] _overlaps = new Collider2D[16];

        // Rigidbody -> time it was first seen overlapping. Cleared once it's free.
        private readonly Dictionary<Rigidbody2D, float> _seenSince = new Dictionary<Rigidbody2D, float>();
        private readonly List<Rigidbody2D> _toForget = new List<Rigidbody2D>();

        private float _nextScan;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();

            _filter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = true,
                layerMask = ejectLayers,
            };
        }

        private void FixedUpdate()
        {
            if (_col == null) return;

            // Rescan overlaps on the configured cadence.
            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + Mathf.Max(0f, scanInterval);
                Rescan();
            }

            LiftStuck();
        }

        // Refresh the set of overlapping rigidbodies and when we first saw each.
        private void Rescan()
        {
            int count = _col.Overlap(_filter, _overlaps);

            // Mark everything currently tracked as a candidate to forget; anything still
            // overlapping will be un-marked below.
            _toForget.Clear();
            foreach (var rb in _seenSince.Keys) _toForget.Add(rb);

            for (int i = 0; i < count; i++)
            {
                var other = _overlaps[i];
                if (other == null) continue;

                var rb = other.attachedRigidbody;
                if (rb == null) continue;
                if (rb.gameObject == gameObject) continue;              // ignore self
                if (!PassesFilter(other)) continue;

                if (!_seenSince.ContainsKey(rb))
                    _seenSince[rb] = Time.time;

                _toForget.Remove(rb); // still here, keep it
            }

            // Drop anything that's no longer overlapping.
            for (int i = 0; i < _toForget.Count; i++)
                _seenSince.Remove(_toForget[i]);
        }

        private bool PassesFilter(Collider2D other)
        {
            if ((ejectLayers.value & (1 << other.gameObject.layer)) == 0) return false;
            if (itemsOnly && other.GetComponentInParent<Item>() == null) return false;
            return true;
        }

        // For every rigidbody stuck past the grace period, push it up until it's clear
        // of the top edge, then stop tracking it.
        private void LiftStuck()
        {
            if (_seenSince.Count == 0) return;

            float topEdge = _col.bounds.max.y;

            _toForget.Clear();

            foreach (var kv in _seenSince)
            {
                var rb = kv.Key;
                if (rb == null) { _toForget.Add(rb); continue; }

                // Not stuck long enough yet.
                if (Time.time - kv.Value < graceSeconds) continue;

                // Skip anything the player is currently holding, if the game marks that.
                var item = rb.GetComponent<Item>();
                if (item != null && item.IsDragging) { _toForget.Add(rb); continue; }

                float objBottom = ObjectBottom(rb);

                // Cleared the top? Done — forget it.
                if (objBottom >= topEdge + clearance)
                {
                    _toForget.Add(rb);
                    continue;
                }

                // Lift straight up. Move the body kinematically-ish via velocity so it
                // reads as physics, not a teleport.
                Vector2 v = rb.linearVelocity;
                if (cancelDownwardVelocity && v.y < 0f) v.y = 0f;
                v.y = Mathf.Max(v.y, ejectSpeed);
                rb.linearVelocity = v;
            }

            for (int i = 0; i < _toForget.Count; i++)
                _seenSince.Remove(_toForget[i]);
        }

        // Bottom Y of the object's own collider (falls back to its position).
        private static float ObjectBottom(Rigidbody2D rb)
        {
            var c = rb.GetComponent<Collider2D>();
            if (c != null) return c.bounds.min.y;
            return rb.position.y;
        }

        private void OnDrawGizmosSelected()
        {
            var c = GetComponent<Collider2D>();
            if (c == null) return;

            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            Vector3 topLeft = new Vector3(c.bounds.min.x, c.bounds.max.y + clearance, 0f);
            Vector3 topRight = new Vector3(c.bounds.max.x, c.bounds.max.y + clearance, 0f);
            Gizmos.DrawLine(topLeft, topRight);
        }
    }
}