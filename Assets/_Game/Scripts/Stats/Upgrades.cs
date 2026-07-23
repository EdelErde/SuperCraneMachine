using NekoLab.Stats;

namespace CraneMachine
{
    #region Bases

    public abstract class ActivateObjectUpgrade : Upgrade
    {
        protected abstract UnlockTarget Target { get; }
        protected override int MaxPurchases => 1;

        protected override void ApplyEffect()
            => SceneRef.SetActive(Target, true);
    }

    public abstract class UnlockItemUpgrade<T> : Upgrade where T : ItemType
    {
        protected override int MaxPurchases => 1;

        protected override void ApplyEffect() =>
            Item<T>(ItemStat.Unlocked).AddModifier(new StatModifier(1f, StatModifierEffect.Add));
    }

    #endregion

    #region Drag

    public class HandStrengthUpgrade : Upgrade
    {
        protected override string Name => "Stronger Hands";
        protected override int BaseCost => 15;
        protected override float CostMultiplier => 1.4f;
        protected override string Icon => "UpgradeIcons/Hand";
        protected override int MaxPurchases => 10;
        protected override void ApplyEffect() =>
            Game(GameStat.HandStrength).AddModifier(new StatModifier(.3f, StatModifierEffect.Add));
    }

    public class DragCountUpgrade : Upgrade
    {
        protected override string Name => "Extra Hand";
        protected override int BaseCost => 60;
        protected override float CostMultiplier => 1.7f;
        protected override int MaxPurchases => 4;
        protected override void ApplyEffect() =>
            Game(GameStat.DragCount).AddModifier(new StatModifier(1f, StatModifierEffect.Add));
    }

    public class DragRadiusUpgrade : Upgrade
    {
        protected override string Name => "Wider Reach";
        protected override int BaseCost => 35;
        protected override float CostMultiplier => 1.5f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.DragRadius).AddModifier(new StatModifier(0.25f, StatModifierEffect.Add));
    }

    #endregion

    #region Claw

    public class ClawSpeedUpgrade : Upgrade
    {
        protected override string Name => "Faster Claw";
        protected override int BaseCost => 40;
        protected override float CostMultiplier => 1.55f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.ClawSweepSpeed).AddModifier(new StatModifier(0.5f, StatModifierEffect.Add));
    }

    public class ClawLiftSpeedUpgrade : Upgrade
    {
        protected override string Name => "Faster Lift";
        protected override int BaseCost => 45;
        protected override float CostMultiplier => 1.55f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.ClawVerticalSpeed).AddModifier(new StatModifier(0.75f, StatModifierEffect.Add));
    }

    public class GrabStrengthUpgrade : Upgrade
    {
        protected override string Name => "Stronger Grip";
        protected override int BaseCost => 50;
        protected override float CostMultiplier => 1.5f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.GrabStrength).AddModifier(new StatModifier(25f, StatModifierEffect.Add));
    }

    #endregion

    #region Spawn

    public class FasterRainUpgrade : Upgrade
    {
        protected override string Name => "Faster Items";
        protected override int BaseCost => 80;
        protected override float CostMultiplier => 1.55f;
        protected override int MaxPurchases => 7;
        protected override void ApplyEffect() =>
            Game(GameStat.SpawnInterval).AddModifier(new StatModifier(-0.28f, StatModifierEffect.Add));
    }

    public class MoreItemsUpgrade : Upgrade
    {
        protected override string Name => "More Items";
        protected override int BaseCost => 110;
        protected override float CostMultiplier => 1.65f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.MaxLiveItems).AddModifier(new StatModifier(3, StatModifierEffect.Add));
    }

    #endregion

    #region Money

    public class MoneyMultiplierUpgrade : Upgrade
    {
        protected override string Name => "Better Prices";
        protected override int BaseCost => 45;
        protected override float CostMultiplier => 1.55f;
        protected override int MaxPurchases => 10;
        protected override void ApplyEffect() =>
            Game(GameStat.MoneyMultiplier).AddModifier(new StatModifier(0.15f, StatModifierEffect.Add));
    }

    #endregion

    #region Item values

    public class EggValueUpgrade : Upgrade
    {
        protected override string Name => "Golden Eggs";
        protected override int BaseCost => 30;
        protected override float CostMultiplier => 1.5f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Item<Egg>(ItemStat.SellValue).AddModifier(new StatModifier(4f, StatModifierEffect.Add));
    }

    public class TeddyValueUpgrade : Upgrade
    {
        protected override string Name => "Plush Premium";
        protected override int BaseCost => 70;
        protected override float CostMultiplier => 1.5f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Item<TeddyBear>(ItemStat.SellValue).AddModifier(new StatModifier(10f, StatModifierEffect.Add));
    }

    public class ScrapValueUpgrade : Upgrade
    {
        protected override string Name => "Scrap Dealer";
        protected override int BaseCost => 25;
        protected override float CostMultiplier => 1.45f;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Item<TinCan>(ItemStat.SellValue).AddModifier(new StatModifier(3f, StatModifierEffect.Add));
    }

    public class DiamondValueUpgrade : Upgrade
    {
        protected override string Name => "Diamond Polish";
        protected override int BaseCost => 400;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Item<Diamond>(ItemStat.SellValue).AddModifier(new StatModifier(40f, StatModifierEffect.Add));
    }

    #endregion

    #region Item weight

    public class LighterItemsUpgrade : Upgrade
    {
        protected override string Name => "Featherweight";
        protected override int BaseCost => 350;
        protected override float CostMultiplier => 1.7f;
        protected override int MaxPurchases => 4;
        protected override void ApplyEffect() =>
            Item<Diamond>(ItemStat.Mass).AddModifier(new StatModifier(-0.15f, StatModifierEffect.Add));
    }

    #endregion

    #region Item unlocks

    public class UnlockTeddyUpgrade : UnlockItemUpgrade<TeddyBear>
    {
        protected override string Name => "Teddy Bears";
        protected override int BaseCost => 90;
    }

    public class UnlockDiamondUpgrade : UnlockItemUpgrade<Diamond>
    {
        protected override string Name => "Diamonds";
        protected override int BaseCost => 1400;
    }

    #endregion

    #region Object unlocks

    public class UnlockClawUpgrade : ActivateObjectUpgrade
    {
        protected override string Name => "Claw";
        protected override int BaseCost => 250;
        protected override UnlockTarget Target => UnlockTarget.Claw;
    }

    public class UnlockConveyorUpgrade : ActivateObjectUpgrade
    {
        protected override string Name => "Conveyor Belt";
        protected override int BaseCost => 900;
        protected override UnlockTarget Target => UnlockTarget.Conveyor;
    }

    public class UnlockAutoSeller : ActivateObjectUpgrade
    {
        protected override string Name => "AutoSeller";
        protected override int BaseCost => 2500;
        protected override UnlockTarget Target => UnlockTarget.AutoSeller;
    }

    #endregion
}