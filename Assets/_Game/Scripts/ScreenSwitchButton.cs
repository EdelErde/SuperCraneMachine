using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // Button that switches to a screen by raising its CinemachineCamera's priority
    // (see ScreenCameraRef.Activate). Disabled (not interactable) while the target
    // screen is locked, so the player can't switch to a screen that isn't unlocked yet.
    [RequireComponent(typeof(Button))]
    public class ScreenSwitchButton : MonoBehaviour
    {
        [SerializeField] private ScreenId target;

        private Button _button;

        private void Awake() => _button = GetComponent<Button>();

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClick);

            if (ServiceLocator.ScreenUnlocks != null)
                ServiceLocator.ScreenUnlocks.OnScreenUnlocked += HandleScreenUnlocked;

            RefreshInteractable();
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClick);

            if (ServiceLocator.ScreenUnlocks != null)
                ServiceLocator.ScreenUnlocks.OnScreenUnlocked -= HandleScreenUnlocked;
        }

        private void HandleClick() => ScreenCameraRef.Activate(target);

        private void HandleScreenUnlocked(ScreenId unlocked)
        {
            if (unlocked == target) RefreshInteractable();
        }

        private void RefreshInteractable()
        {
            bool unlocked = ServiceLocator.ScreenUnlocks == null || ServiceLocator.ScreenUnlocks.IsUnlocked(target);
            _button.interactable = unlocked;
        }
    }
}