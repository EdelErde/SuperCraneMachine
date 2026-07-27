using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraneMachine
{
    public class MagnetInput : MonoBehaviour
    {
        private static bool _grabQueued;
        public static bool GrabPressed => _grabQueued;

        public static bool ConsumeGrab()
        {
            if (!_grabQueued) return false;
            _grabQueued = false;
            return true;
        }

        private void Update()
        {
            bool pressed;
#if ENABLE_INPUT_SYSTEM
            pressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            pressed = Input.GetKeyDown(KeyCode.Space);
#endif
            if (pressed) _grabQueued = true;
        }
    }
}