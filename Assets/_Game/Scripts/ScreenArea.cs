using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Defines the world-space rectangle a screen occupies, so anything can ask "which
    // screen is this world point on?" or "list the items on this screen." Place one on
    // each screen root (the same object that has the ScreenRef), sized to cover that
    // screen's play area.
    //
    // WHY THIS EXISTS: items are parentless loose rigidbodies with no idea what screen
    // they're on. Rather than reparenting every item or tracking membership per-item, we
    // define each screen's bounds once here and test item positions against it — an exact
    // rectangle-contains-point check, no radius, no bookkeeping. Mirrors the static-
    // registry pattern of ScreenRef / ScreenCameraRef so it feels the same to author.
    //
    // The area comes from a BoxCollider2D on this object if present (author it visually,
    // exactly like ItemSpawner's spawnArea); otherwise from the size/offset fields below.
    [DisallowMultipleComponent]
    public class ScreenArea : MonoBehaviour
    {
        [SerializeField] private ScreenId screen;

        [Tooltip("If set, the screen's area is this collider's bounds (author it visually). " +
                 "If left empty, the Size/Center fields below are used instead.")]
        [SerializeField] private BoxCollider2D areaCollider;

        [Header("Fallback area (used only if no Area Collider is set)")]
        [Tooltip("Size of the screen area in world units (width, height), centered on Center.")]
        [SerializeField] private Vector2 size = new Vector2(20f, 12f);
        [Tooltip("Center of the fallback area, as a world offset from this object.")]
        [SerializeField] private Vector2 center = Vector2.zero;

        private static readonly Dictionary<ScreenId, List<ScreenArea>> _refs
            = new Dictionary<ScreenId, List<ScreenArea>>();

        public ScreenId Screen => screen;

        private void Awake()
        {
            if (areaCollider == null) areaCollider = GetComponent<BoxCollider2D>();
            Register(screen, this);
        }

        private void OnDestroy()
        {
            if (_refs.TryGetValue(screen, out var list))
            {
                list.Remove(this);
                if (list.Count == 0) _refs.Remove(screen);
            }
        }

        private static void Register(ScreenId screen, ScreenArea a)
        {
            if (!_refs.TryGetValue(screen, out var list))
            {
                list = new List<ScreenArea>();
                _refs[screen] = list;
            }
            if (!list.Contains(a)) list.Add(a);
        }

        // World-space rectangle this screen covers.
        public Bounds Bounds
        {
            get
            {
                if (areaCollider != null) return areaCollider.bounds;
                Vector2 c = (Vector2)transform.position + center;
                return new Bounds(new Vector3(c.x, c.y, 0f), new Vector3(size.x, size.y, 0f));
            }
        }

        public bool Contains(Vector2 worldPoint)
        {
            Bounds b = Bounds;
            return worldPoint.x >= b.min.x && worldPoint.x <= b.max.x &&
                   worldPoint.y >= b.min.y && worldPoint.y <= b.max.y;
        }

        // The first registered ScreenArea for a screen (there's normally exactly one).
        public static ScreenArea For(ScreenId screen)
        {
            if (_refs.TryGetValue(screen, out var list))
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null) return list[i];
            return null;
        }

        // Which screen a given world point falls on, or null if it's outside every screen
        // area. Useful later for auto-detecting an item's / drone's screen.
        public static ScreenId? ScreenOf(Vector2 worldPoint)
        {
            foreach (var kv in _refs)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null && list[i].Contains(worldPoint))
                        return kv.Key;
            }
            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Bounds b = Bounds;
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}