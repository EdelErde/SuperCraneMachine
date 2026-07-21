using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraneMachine
{
    // Central input access. Frontend just reads ClawInput.GrabPressed.
    public class ClawInput : MonoBehaviour
    {
        public static bool GrabPressed { get; private set; }

        private void Update()
        {
            bool pressed;
#if ENABLE_INPUT_SYSTEM
            pressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            pressed = Input.GetKeyDown(KeyCode.Space);
#endif
            GrabPressed = pressed;
        }
    }
}
