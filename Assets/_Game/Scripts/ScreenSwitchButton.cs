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

        private void Update()
        {
            // ScreenUnlockService may Awake() after this button (script execution
            // order between independent components isn't guaranteed) — keep checking
            // until we've successfully subscribed to it, so a locked screen doesn't
            // stay clickable just because this button enabled first.
            if (_subscribed || ServiceLocator.ScreenUnlocks == null) return;

            ServiceLocator.ScreenUnlocks.OnScreenUnlocked += HandleScreenUnlocked;
            _subscribed = true;
            RefreshInteractable();
        }

        private bool _subscribed;

        private void HandleClick() => ScreenCameraRef.Activate(target);

        private void HandleScreenUnlocked(ScreenId unlocked)
        {
            if (unlocked == target) RefreshInteractable();
        }

        private void RefreshInteractable()
        {
            // No fallback to "unlocked" here — if the service isn't around yet, the
            // button stays locked until it is (see Update()'s late-subscribe check)
            // rather than defaulting to clickable.
            bool unlocked = ServiceLocator.ScreenUnlocks != null && ServiceLocator.ScreenUnlocks.IsUnlocked(target);
            _button.interactable = unlocked;
        }
    }
}