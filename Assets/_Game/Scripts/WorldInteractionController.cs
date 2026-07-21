using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraneMachine
{
    public class WorldInteractionController : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [Tooltip("Only these layers are checked for draggables.")]
        [SerializeField] private LayerMask interactableMask = ~0;

        private IDraggable _held;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            Vector2 pointer = PointerWorld();

            if (PressedThisFrame())
                TryPick(pointer);

            if (_held != null)
            {
                if (_held.Transform == null)
                {
                    _held = null;
                }
                else if (ReleasedThisFrame())
                {
                    _held.OnDragEnd();
                    _held = null;
                }
                else
                {
                    _held.OnDrag(pointer);
                }
            }
        }

        private void TryPick(Vector2 worldPoint)
        {
            var hit = Physics2D.OverlapPoint(worldPoint, interactableMask);
            if (hit == null) return;

            var draggable = hit.GetComponentInParent<IDraggable>();
            if (draggable == null || !draggable.CanDrag) return;

            _held = draggable;
            _held.OnDragBegin();
        }

        private Vector2 PointerWorld()
        {
            Vector3 screen = PointerScreen();
            return cam.ScreenToWorldPoint(screen);
        }

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