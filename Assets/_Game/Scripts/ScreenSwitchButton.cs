using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // Button that switches to a screen by raising its CinemachineCamera's priority
    // (see ScreenCameraRef.Activate).
    //
    // Visibility rules:
    //  - A button whose target screen is still LOCKED is hidden.
    //  - If there is only ONE switchable screen available (only one distinct unlocked
    //    target across all switch buttons), ALL switch buttons hide — there's nothing to
    //    switch to, so screen-switching is only shown once a SECOND screen unlocks.
    //  - The button for the CURRENTLY ACTIVE screen shows its pressed-state sprite
    //    (reusing the Button's SpriteSwap pressedSprite). Non-active buttons show normal.
    //
    // WHY WE HIDE VIA CanvasGroup INSTEAD OF SetActive(false) (this was the bug):
    // The old version disabled the whole GameObject to hide a button. But a disabled
    // GameObject removes itself from _all (in OnDisable) and can't receive events. At
    // startup only ONE screen is unlocked, so the count is 1 and EVERY button disabled
    // itself — which emptied _all and killed every subscription. When the 2nd screen
    // later unlocked, there was no live, registered button left to hear OnScreenUnlocked
    // or to be counted, so nothing ever re-appeared: a permanent deadlock.
    //
    // Fix: the GameObject stays ACTIVE and registered/subscribed for its whole life. We
    // hide it purely visually with a CanvasGroup (alpha + raycast block) and drop it out
    // of layout with a LayoutElement.ignoreLayout, so a hidden button still takes no
    // space AND still hears unlocks. The component is always in _all, always counted,
    // always able to react.
    //
    // Timing: Unity doesn't guarantee Awake() order between independent components, so
    // ScreenUnlockService may Awake after this button. We subscribe to the static
    // ScreenUnlockService.Ready event (fired at the end of the service's Awake) so we
    // correct our state once the service is actually ready, regardless of order. This is
    // now reliable because we never deactivate ourselves — the subscription set up in
    // Awake() always stays live.
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenSwitchButton : MonoBehaviour
    {
        [SerializeField] private ScreenId target;

        [Tooltip("Image whose sprite is swapped to show the active-screen 'pressed' look. " +
                 "Defaults to the Button's targetGraphic (usually this object's Image).")]
        [SerializeField] private Image targetImage;

        [Tooltip("If ON, hide this button whenever only one switchable screen is available " +
                 "(nothing to switch to). Turn OFF for a button that should always show.")]
        [SerializeField] private bool hideWhenOnlyOneScreen = true;

        // Every live switch button, so any one of them can ask "how many distinct screens
        // are currently switchable?" without a central manager. Buttons stay in here for
        // their whole lifetime now (added in Awake, removed in OnDestroy) — NOT tied to
        // enabled state, so a hidden button is still counted and still notified.
        private static readonly List<ScreenSwitchButton> _all = new List<ScreenSwitchButton>();

        private Button _button;
        private CanvasGroup _group;
        private LayoutElement _layoutElement;
        private Sprite _normalSprite;   // captured from the Image at startup
        private Sprite _pressedSprite;  // read from the Button's SpriteState
        private bool _spriteCaptured;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(HandleClick);

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

            // Optional: lets a hidden button drop out of a layout group so it takes no
            // space. Added on demand; harmless if the button isn't under a layout group.
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null) _layoutElement = gameObject.AddComponent<LayoutElement>();

            if (targetImage == null)
                targetImage = _button != null ? _button.targetGraphic as Image : GetComponent<Image>();

            CaptureSprites();

            // Register ONCE for the object's whole life — not in OnEnable/OnDisable — so
            // visibility never removes us from the count or the notification set.
            if (!_all.Contains(this)) _all.Add(this);

            ScreenUnlockService.Ready += HandleServiceReady;
            ScreenCameraRef.OnActivated += HandleScreenActivated;

            if (ServiceLocator.ScreenUnlocks != null)
                ServiceLocator.ScreenUnlocks.OnScreenUnlocked += HandleScreenUnlocked;

            RefreshAllPressedAndVisibility();
        }

        private void OnDestroy()
        {
            _all.Remove(this);

            ScreenUnlockService.Ready -= HandleServiceReady;
            ScreenCameraRef.OnActivated -= HandleScreenActivated;

            if (ServiceLocator.ScreenUnlocks != null)
                ServiceLocator.ScreenUnlocks.OnScreenUnlocked -= HandleScreenUnlocked;
        }

        private void CaptureSprites()
        {
            if (_spriteCaptured) return;
            if (targetImage != null) _normalSprite = targetImage.sprite;
            if (_button != null) _pressedSprite = _button.spriteState.pressedSprite;
            _spriteCaptured = true;
        }

        private void HandleClick() => ScreenCameraRef.Activate(target);

        private void HandleScreenUnlocked(ScreenId unlocked)
        {
            // Any unlock can change both this button's own visibility AND the total
            // available-screen count, so re-evaluate everyone.
            RefreshAllPressedAndVisibility();
        }

        private void HandleScreenActivated(ScreenId active)
        {
            // The active screen changed — update pressed-sprite state on all buttons.
            RefreshAllPressed();
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

            RefreshAllPressedAndVisibility();
        }

        // ---- visibility --------------------------------------------------------

        private bool IsTargetUnlocked()
        {
            return ServiceLocator.ScreenUnlocks != null
                && ServiceLocator.ScreenUnlocks.IsUnlocked(target);
        }

        // How many DISTINCT screens are currently switchable (have an unlocked button)?
        // If <=1, there's nowhere to switch to, so all buttons hide. Every button — hidden
        // or shown — is still in _all, so this count is always correct.
        private static int DistinctAvailableScreens()
        {
            var seen = new HashSet<ScreenId>();
            for (int i = 0; i < _all.Count; i++)
            {
                var b = _all[i];
                if (b == null) continue;
                if (b.IsTargetUnlocked()) seen.Add(b.target);
            }
            return seen.Count;
        }

        private void RefreshActive()
        {
            // Hidden if the target screen is locked, OR if only one screen is available.
            bool unlocked = IsTargetUnlocked();
            bool enoughScreens = !hideWhenOnlyOneScreen || DistinctAvailableScreens() > 1;
            bool shouldShow = unlocked && enoughScreens;

            SetShown(shouldShow);
            if (shouldShow) RefreshPressed();
        }

        // Hide/show WITHOUT disabling the GameObject, so the component keeps running,
        // stays in _all, and keeps its event subscriptions.
        private void SetShown(bool shown)
        {
            if (_group != null)
            {
                _group.alpha = shown ? 1f : 0f;
                _group.interactable = shown;
                _group.blocksRaycasts = shown;
            }
            // Drop out of any layout group when hidden so it occupies no space (mirrors
            // the old SetActive(false) footprint behaviour).
            if (_layoutElement != null)
                _layoutElement.ignoreLayout = !shown;
        }

        // Re-evaluate visibility for every button (used when the available set changes).
        private static void RefreshAllPressedAndVisibility()
        {
            // Snapshot in case the list changes mid-iteration (e.g. a button destroyed).
            var snapshot = _all.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                if (snapshot[i] != null) snapshot[i].RefreshActive();
        }

        // ---- pressed-sprite (current screen) ----------------------------------

        private static void RefreshAllPressed()
        {
            for (int i = 0; i < _all.Count; i++)
                if (_all[i] != null) _all[i].RefreshPressed();
        }

        // Show the pressed sprite when THIS button's screen is the active one.
        private void RefreshPressed()
        {
            if (targetImage == null) return;
            CaptureSprites();

            bool isCurrent = ScreenCameraRef.Current == target;
            Sprite want = (isCurrent && _pressedSprite != null) ? _pressedSprite : _normalSprite;

            if (want != null && targetImage.sprite != want)
                targetImage.sprite = want;
        }
    }
}