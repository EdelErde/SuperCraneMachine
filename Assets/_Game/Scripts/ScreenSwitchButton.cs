using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // Button that switches to a screen by raising its CinemachineCamera's priority
    // (see ScreenCameraRef.Activate). The whole GameObject is switched OFF while the
    // target screen is locked (not just made non-interactable) — so a locked screen's
    // button doesn't render or take up space at all until unlocked.
    //
    // Timing: Unity does NOT guarantee Awake() order between independent components,
    // so ScreenUnlockService.Awake() might run before OR after this button's Awake().
    // To handle "after" safely (this button would otherwise read a not-yet-ready
    // ServiceLocator.ScreenUnlocks as null and deactivate itself, then have no way to
    // hear about the real state since a disabled GameObject can't poll/Update()), this
    // also subscribes to the static ScreenUnlockService.Ready event, which the service
    // fires once at the end of its own Awake() — that always reaches every button
    // regardless of which Awake() ran first, since it's a static subscription set up
    // here in THIS component's own Awake() (which always runs once, even for an
    // object that deactivates itself moments later in the same call).
    public class ScreenSwitchButton : MonoBehaviour
    {
        [SerializeField] private ScreenId target;

        private void Awake()
        {
            var button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(HandleClick);

            ScreenUnlockService.Ready += HandleServiceReady;

            if (ServiceLocator.ScreenUnlocks != null)
                ServiceLocator.ScreenUnlocks.OnScreenUnlocked += HandleScreenUnlocked;

            RefreshActive();
        }

        private void OnDestroy()
        {
            ScreenUnlockService.Ready -= HandleServiceReady;

            if (ServiceLocator.ScreenUnlocks != null)
                ServiceLocator.ScreenUnlocks.OnScreenUnlocked -= HandleScreenUnlocked;
        }

        private void HandleClick() => ScreenCameraRef.Activate(target);

        private void HandleScreenUnlocked(ScreenId unlocked)
        {
            if (unlocked == target) RefreshActive();
        }

        // Fired once ScreenUnlockService has actually finished computing the initial
        // unlocked set — re-subscribe to OnScreenUnlocked in case Awake() missed it
        // (ServiceLocator.ScreenUnlocks was still null back then) and re-check state.
        private void HandleServiceReady()
        {
            if (ServiceLocator.ScreenUnlocks != null)
            {
                ServiceLocator.ScreenUnlocks.OnScreenUnlocked -= HandleScreenUnlocked; // avoid double-subscribe
                ServiceLocator.ScreenUnlocks.OnScreenUnlocked += HandleScreenUnlocked;
            }

            RefreshActive();
        }

        private void RefreshActive()
        {
            // No fallback to "unlocked" here — if the service isn't around yet, stay
            // off until HandleServiceReady (or a later OnScreenUnlocked) corrects it.
            bool unlocked = ServiceLocator.ScreenUnlocks != null && ServiceLocator.ScreenUnlocks.IsUnlocked(target);
            if (gameObject.activeSelf != unlocked)
                gameObject.SetActive(unlocked);
        }
    }
}