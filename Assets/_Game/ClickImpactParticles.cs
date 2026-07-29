using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraneMachine
{
    // Spawns a particle burst when you click on a surface that isn't a draggable item.
    public class ClickImpactParticles : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [Tooltip("Which layers count as a clickable surface (walls, background, etc.).")]
        [SerializeField] private LayerMask surfaceMask = ~0;
        [SerializeField] private float burstScale = 1f;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (!PressedThisFrame()) return;

            Vector2 world = cam.ScreenToWorldPoint(PointerScreen());

            // Ignore the click if it landed on a draggable item — that's a grab, not a wall hit.
            var overlap = Physics2D.OverlapPoint(world);
            if (overlap != null && overlap.GetComponentInParent<IDraggable>() != null) return;

            var hit = Physics2D.OverlapPoint(world, surfaceMask);
            if (hit == null) return;

            if (ServiceLocator.Particles != null)
                ServiceLocator.Particles.Play(world, Vector2.up, burstScale);
        }

#if ENABLE_INPUT_SYSTEM
        private static bool PressedThisFrame() =>
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        private static Vector3 PointerScreen() =>
            Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        private static bool PressedThisFrame() => Input.GetMouseButtonDown(0);
        private static Vector3 PointerScreen() => Input.mousePosition;
#endif
    }
}