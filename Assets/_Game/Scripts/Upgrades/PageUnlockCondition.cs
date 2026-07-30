using System;
using UnityEngine;

namespace CraneMachine
{
    // A self-contained, evaluable page-unlock rule. Mirrors UpgradePageDefinition's fields
    // but lives on the runtime page so it can be checked and described without editor code.
    [Serializable]
    public class PageUnlockCondition
    {
        public PageUnlockMode mode = PageUnlockMode.Always;

        [SerializeReference, UpgradeReference] public IUpgrade unlockedBy;
        [Min(1)] public int requiredLevel = 1;
        [Min(1)] public int requiredUpgradeCount = 1;

        public bool IsMet
        {
            get
            {
                var svc = ServiceLocator.UpgradeService;
                switch (mode)
                {
                    case PageUnlockMode.Always:
                        return true;

                    case PageUnlockMode.SpecificUpgrade:
                        if (unlockedBy == null || svc == null) return unlockedBy == null;
                        return svc.TimesPurchased(unlockedBy.GetType()) >= Mathf.Max(1, requiredLevel);

                    case PageUnlockMode.UpgradeCount:
                        if (svc == null) return false;
                        return svc.TotalPurchases() >= Mathf.Max(1, requiredUpgradeCount);

                    default:
                        return true;
                }
            }
        }

        // Human-readable requirement shown on locked tabs/pages.
        public string Describe()
        {
            switch (mode)
            {
                case PageUnlockMode.SpecificUpgrade:
                    string name = unlockedBy != null ? unlockedBy.DisplayName : "?";
                    return requiredLevel > 1
                        ? $"Requires {name} Lv.{requiredLevel}"
                        : $"Requires {name}";

                case PageUnlockMode.UpgradeCount:
                    return $"Requires {requiredUpgradeCount} upgrades";

                default:
                    return string.Empty;
            }
        }

        // Progress 0..1 toward unlocking (for optional tab fill visuals).
        public float Progress
        {
            get
            {
                var svc = ServiceLocator.UpgradeService;
                if (svc == null) return 0f;
                switch (mode)
                {
                    case PageUnlockMode.Always:
                        return 1f;
                    case PageUnlockMode.SpecificUpgrade:
                        if (unlockedBy == null) return 1f;
                        return Mathf.Clamp01(svc.TimesPurchased(unlockedBy.GetType()) / (float)Mathf.Max(1, requiredLevel));
                    case PageUnlockMode.UpgradeCount:
                        return Mathf.Clamp01(svc.TotalPurchases() / (float)Mathf.Max(1, requiredUpgradeCount));
                    default:
                        return 1f;
                }
            }
        }
    }
}
