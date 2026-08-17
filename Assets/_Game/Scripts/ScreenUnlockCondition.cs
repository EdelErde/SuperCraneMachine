using System;
using UnityEngine;

namespace CraneMachine
{
    // One condition that must be satisfied for a screen to unlock. Polymorphic (like
    // SortRule.type/ItemType) so new condition kinds can be added later without
    // touching ScreenUnlockRule or the service that evaluates them.
    [Serializable]
    public abstract class ScreenUnlockCondition
    {
        public abstract bool IsMet();
    }

    // Met once a specific upgrade has been purchased at least once (or 'timesRequired'
    // times, for repeatable upgrades). Set 'upgradeTypeName' to the upgrade class name
    // (e.g. "UnlockSortingMachineUpgrade") — stored as a string since IUpgrade
    // instances aren't Unity-serializable the way ItemType/SortRule are. The
    // [UpgradeTypeName] attribute gives it a searchable dropdown in the inspector
    // (see UpgradeTypeNameDrawer) instead of free-typing the class name.
    [Serializable]
    public class UpgradePurchasedCondition : ScreenUnlockCondition
    {
        [UpgradeTypeName]
        [Tooltip("Which upgrade must be purchased.")]
        public string upgradeTypeName;
        [Tooltip("How many times it must have been purchased. 1 = just needs to be bought once.")]
        public int timesRequired = 1;

        public override bool IsMet()
        {
            if (string.IsNullOrEmpty(upgradeTypeName) || ServiceLocator.UpgradeService == null)
                return false;

            var type = Type.GetType($"CraneMachine.{upgradeTypeName}");
            if (type == null) return false;

            return ServiceLocator.UpgradeService.TimesPurchased(type) >= Mathf.Max(1, timesRequired);
        }
    }

    // Met once a GameStat reaches (or exceeds) a threshold value.
    [Serializable]
    public class GameStatThresholdCondition : ScreenUnlockCondition
    {
        public GameStat stat;
        public float threshold;

        public override bool IsMet()
        {
            if (ServiceLocator.StatService == null) return false;
            return ServiceLocator.StatService.GameValue(stat) >= threshold;
        }
    }

    // What lifetime running total to check for LifetimeTotalCondition. Deliberately a
    // small dedicated set rather than reusing GameStat, since these are cumulative
    // totals (StatService.TotalMoneyEarned/TotalFuelProduced) and not live/spendable
    // stat values.
    public enum LifetimeTotal { MoneyEarned, FuelProduced }

    // Met once a lifetime running total (money ever earned, fuel ever produced — never
    // decreases when spent, unlike CurrentMoney/Fuel) reaches a threshold.
    [Serializable]
    public class LifetimeTotalCondition : ScreenUnlockCondition
    {
        public LifetimeTotal total;
        public float threshold;

        public override bool IsMet()
        {
            if (ServiceLocator.StatService == null) return false;

            float value;
            switch (total)
            {
                case LifetimeTotal.MoneyEarned: value = ServiceLocator.StatService.TotalMoneyEarned; break;
                case LifetimeTotal.FuelProduced: value = ServiceLocator.StatService.TotalFuelProduced; break;
                default: value = 0f; break;
            }
            return value >= threshold;
        }
    }
}