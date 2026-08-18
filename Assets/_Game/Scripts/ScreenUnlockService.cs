using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Skeleton for unlocking screens. Configure a list of rules (screen + conditions);
    // once a rule's conditions are all met, this calls ScreenRef.SetActive(screen,
    // true) on the matching GameObject(s) — same mechanism ActivateObjectUpgrade uses
    // for individual machine unlocks (SceneRef), just re-evaluated continuously here
    // instead of firing once from a single upgrade purchase, since a screen's
    // condition can be a stat threshold that's crossed passively over time.
    //
    // NOT covered by this skeleton (left for later, per the brief):
    //   - How the player actually gets FROM one screen TO another (camera pan, swipe,
    //     scroll, a screen-select UI, etc.) — this class only flips visibility once
    //     unlocked. Newly-unlocked screens simply become active GameObjects; nothing
    //     here moves the camera to them or presents them to the player.
    //   - Any "just unlocked!" notification/fanfare — OnScreenUnlocked is exposed for
    //     that to hook into later.
    //
    // Re-checks are cheap (a handful of rules, each a handful of conditions) so this
    // just polls every frame rather than wiring into every possible event source
    // (upgrades, stat changes, fuel changes, ...). Swap to event-driven later if the
    // rule list grows large enough for that to matter.
    public class ScreenUnlockService : MonoBehaviour
    {
        [SerializeField] private List<ScreenUnlockRule> rules = new List<ScreenUnlockRule>();

        private readonly HashSet<ScreenId> _unlocked = new HashSet<ScreenId>();

        public event System.Action<ScreenId> OnScreenUnlocked;

        private void Awake()
        {
            ServiceLocator.ScreenUnlocks = this;

            // Any ScreenId with no rule in 'rules' has no unlock condition at all —
            // treat it as unlocked from the start (this is how you mark a screen as
            // "always available", e.g. your starting screen: just don't give it a
            // rule). Only screens that actually have a rule targeting them start
            // locked, pending that rule's conditions.
            var ruled = new HashSet<ScreenId>();
            if (rules != null)
                foreach (var rule in rules)
                    if (rule != null) ruled.Add(rule.screen);

            foreach (ScreenId screen in System.Enum.GetValues(typeof(ScreenId)))
                if (!ruled.Contains(screen)) _unlocked.Add(screen);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.ScreenUnlocks == this)
                ServiceLocator.ScreenUnlocks = null;
        }

        public bool IsUnlocked(ScreenId screen) => _unlocked.Contains(screen);

        // Force-unlock a screen outside the normal rule evaluation (debug/testing, or
        // a designer override). Idempotent.
        public void Unlock(ScreenId screen)
        {
            if (!_unlocked.Add(screen)) return;
            ScreenRef.SetActive(screen, true);
            OnScreenUnlocked?.Invoke(screen);
        }

        private void Update()
        {
            if (rules == null) return;

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null || _unlocked.Contains(rule.screen)) continue;
                if (rule.IsMet()) Unlock(rule.screen);
            }
        }
    }
}