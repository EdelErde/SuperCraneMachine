using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraneMachine
{
    public class WorldInteractionController : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private LayerMask interactableMask = ~0;

        private readonly List<IDraggable> _held = new List<IDraggable>();
        private readonly List<IDraggable> _candidates = new List<IDraggable>();
        private readonly Collider2D[] _hits = new Collider2D[32];

        public IReadOnlyList<IDraggable> Held => _held;
        public Vector2 PointerWorldPosition { get; private set; }

        private int DragCount =>
            ServiceLocator.StatService != null ? Mathf.RoundToInt(ServiceLocator.StatService.GameValue(GameStat.DragCount)) : 1;
        private float DragRadius =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.DragRadius) : 0.75f;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            Vector2 pointer = PointerWorld();
            PointerWorldPosition = pointer;

            if (PressedThisFrame())
                Pick(pointer);

            if (_held.Count > 0)
            {
                if (ReleasedThisFrame())
                {
                    ReleaseAll();
                }
                else
                {
                    for (int i = _held.Count - 1; i >= 0; i--)
                    {
                        var d = _held[i];
                        if (d.Transform == null || !d.IsDragging)   // destroyed or slipped
                        {
                            _held.RemoveAt(i);
                            continue;
                        }
                        d.OnDrag(pointer);
                    }
                }
            }

            UpdateCursor(pointer);
        }

        private void UpdateCursor(Vector2 pointer)
        {
            if (ServiceLocator.CursorManager == null) return;

            if (_held.Count > 0)
            {
                ServiceLocator.CursorManager.Set(CursorState.Drag);
                return;
            }

            var hit = Physics2D.OverlapPoint(pointer, interactableMask);
            bool overDraggable = hit != null && hit.GetComponentInParent<IDraggable>() is { CanDrag: true };
            ServiceLocator.CursorManager.Set(overDraggable ? CursorState.Hover : CursorState.Default);
        }

        private Vector2 _lastPickPoint;

        private void Pick(Vector2 worldPoint)
        {
            _lastPickPoint = worldPoint;

            float radius = Mathf.Max(0.05f, DragRadius);   // never zero -> avoids pixel-precise clicking
            int count = Physics2D.OverlapCircle(
                worldPoint, radius,
                new ContactFilter2D { useLayerMask = true, layerMask = interactableMask, useTriggers = false },
                _hits);

            int cap = Mathf.Max(1, DragCount);

            // Collect valid draggables, sorted by distance to the click so the
            // nearest item is grabbed first (feels precise but forgiving).
            _candidates.Clear();
            for (int i = 0; i < count; i++)
            {
                var d = _hits[i].GetComponentInParent<IDraggable>();
                if (d == null || !d.CanDrag || _held.Contains(d)) continue;
                if (!_candidates.Contains(d)) _candidates.Add(d);
            }

            _candidates.Sort((a, b) =>
                ((Vector2)a.Transform.position - worldPoint).sqrMagnitude
                .CompareTo(((Vector2)b.Transform.position - worldPoint).sqrMagnitude));

            for (int i = 0; i < _candidates.Count && _held.Count < cap; i++)
            {
                _candidates[i].OnDragBegin();
                _held.Add(_candidates[i]);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float radius = Application.isPlaying ? Mathf.Max(0.05f, DragRadius) : 0.75f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_lastPickPoint, radius);
        }
#endif

        private void ReleaseAll()
        {
            foreach (var d in _held)
                if (d.Transform != null) d.OnDragEnd();
            _held.Clear();
        }

        private Vector2 PointerWorld() => cam.ScreenToWorldPoint(PointerScreen());

#if ENABLE_INPUT_SYSTEM
        private static bool PressedThisFrame() =>
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        private static bool ReleasedThisFrame() =>
            Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        private static Vector3 PointerScreen() =>
            Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        private static bool PressedThisFrame() => Input.GetMouseButtonDown(0);
        private static bool ReleasedThisFrame() => Input.GetMouseButtonUp(0);
        private static Vector3 PointerScreen() => Input.mousePosition;
#endif
    }
}