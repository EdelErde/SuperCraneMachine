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

        [Header("Carry formation")]
        [Tooltip("Ring radius as a fraction of the pickup radius.")]
        [SerializeField] private float ringRadiusScale = 0.6f;
        [Tooltip("Angle of the first slot, in degrees.")]
        [SerializeField] private float ringStartAngle = 90f;

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

            if (ReleasedThisFrame())
            {
                ReleaseAll();
                UpdateCursor(pointer);
                return;
            }

            if (IsHeld())
                Pick(pointer);

            if (_held.Count > 0)
            {
                for (int i = _held.Count - 1; i >= 0; i--)
                {
                    var d = _held[i];
                    // A held item may have been destroyed (e.g. consumed by a hole). Check the
                    // underlying Unity object BEFORE touching d.Transform, since accessing a
                    // member on a destroyed MonoBehaviour throws MissingReferenceException.
                    if (d is UnityEngine.Object obj && obj == null) { _held.RemoveAt(i); continue; }
                    if (d.Transform == null || !d.IsDragging)
                        _held.RemoveAt(i);
                }

                AssignRingSlots(pointer);
            }

            UpdateCursor(pointer);
        }

        private void AssignRingSlots(Vector2 center)
        {
            int n = _held.Count;
            if (n == 0) return;

            if (n == 1)
            {
                _held[0].OnDrag(center);
                return;
            }

            float radius = Mathf.Max(0.05f, DragRadius) * ringRadiusScale;
            float step = Mathf.PI * 2f / n;

            for (int i = 0; i < n; i++)
            {
                float angle = ringStartAngle * Mathf.Deg2Rad + step * i;
                Vector2 slot = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                _held[i].OnDrag(slot);
            }
        }

        private void UpdateCursor(Vector2 pointer)
        {
            if (ServiceLocator.CursorManager == null) return;

            if (_held.Count > 0)
            {
                ServiceLocator.CursorManager.Set(CursorState.Drag);
                return;
            }

            float radius = Mathf.Max(0.05f, DragRadius);
            int count = Physics2D.OverlapCircle(
                pointer, radius,
                new ContactFilter2D { useLayerMask = true, layerMask = interactableMask, useTriggers = false },
                _hits);

            bool inRange = false;
            for (int i = 0; i < count; i++)
            {
                var d = _hits[i].GetComponentInParent<IDraggable>();
                if (d != null && d.CanDrag) { inRange = true; break; }
            }

            ServiceLocator.CursorManager.Set(inRange ? CursorState.Hover : CursorState.Default);
        }

        private Vector2 _lastPickPoint;

        private void Pick(Vector2 worldPoint)
        {
            int cap = Mathf.Max(1, DragCount);
            if (_held.Count >= cap) return;

            _lastPickPoint = worldPoint;

            float radius = Mathf.Max(0.05f, DragRadius);
            int count = Physics2D.OverlapCircle(
                worldPoint, radius,
                new ContactFilter2D { useLayerMask = true, layerMask = interactableMask, useTriggers = false },
                _hits);

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
                _candidates[i].OnDragBegin(worldPoint);
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
            {
                if (d is UnityEngine.Object obj && obj == null) continue;
                if (d.Transform != null) d.OnDragEnd();
            }
            _held.Clear();
        }

        private Vector2 PointerWorld() => cam.ScreenToWorldPoint(PointerScreen());

#if ENABLE_INPUT_SYSTEM
        private static bool PressedThisFrame() =>
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        private static bool ReleasedThisFrame() =>
            Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        private static bool IsHeld() =>
            Mouse.current != null && Mouse.current.leftButton.isPressed;
        private static Vector3 PointerScreen() =>
            Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        private static bool PressedThisFrame() => Input.GetMouseButtonDown(0);
        private static bool ReleasedThisFrame() => Input.GetMouseButtonUp(0);
        private static bool IsHeld() => Input.GetMouseButton(0);
        private static Vector3 PointerScreen() => Input.mousePosition;
#endif
    }
}