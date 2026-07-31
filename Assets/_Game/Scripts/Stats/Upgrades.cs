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

    public abstract class ItemWeightUpgrade<T> : Upgrade where T : ItemType
    {
        protected abstract float WeightPerLevel { get; }
        protected override void ApplyEffect() =>
            Item<T>(ItemStat.Weight).AddModifier(new StatModifier(WeightPerLevel, StatModifierEffect.Add));
    }

    #endregion

    #region Drag

    public class HandStrengthUpgrade : Upgrade
    {
        protected override string Name => "Stronger Hands";
        protected override int BaseCost => 20;
        protected override float CostMultiplier => 1.45f;
        protected override string Icon => "UpgradeIcons/Hand";
        protected override int MaxPurchases => 10;
        protected override void ApplyEffect() =>
            Game(GameStat.HandStrength).AddModifier(new StatModifier(.3f, StatModifierEffect.Add));
    }

    public class DragCountUpgrade : Upgrade
    {
        protected override string Name => "Extra Hand";
        protected override int BaseCost => 80;
        protected override float CostMultiplier => 1.75f;
        protected override int MaxPurchases => 4;
        protected override void ApplyEffect() =>
            Game(GameStat.DragCount).AddModifier(new StatModifier(1f, StatModifierEffect.Add));
    }

    public class DragRadiusUpgrade : Upgrade
    {
        protected override string Name => "Wider Reach";
        protected override int BaseCost => 45;
        protected override float CostMultiplier => 1.5f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.DragRadius).AddModifier(new StatModifier(0.25f, StatModifierEffect.Add));
    }

    #endregion

    #region Magnet

    public class MagnetSpeedUpgrade : Upgrade
    {
        protected override string Name => "Faster Magnet";
        protected override int BaseCost => 60;
        protected override float CostMultiplier => 1.55f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.MagnetSweepSpeed).AddModifier(new StatModifier(0.5f, StatModifierEffect.Add));
    }

    public class MagnetLiftSpeedUpgrade : Upgrade
    {
        protected override string Name => "Faster Lift";
        protected override int BaseCost => 70;
        protected override float CostMultiplier => 1.55f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.MagnetVerticalSpeed).AddModifier(new StatModifier(0.75f, StatModifierEffect.Add));
    }

    public class GrabCapacityUpgrade : Upgrade
    {
        protected override string Name => "Magnet Capacity";
        protected override int BaseCost => 600;
        protected override float CostMultiplier => 2.0f;
        protected override int MaxPurchases => 4;
        protected override void ApplyEffect() =>
            Game(GameStat.MagnetGrabCapacity).AddModifier(new StatModifier(4f, StatModifierEffect.Add));
    }

    public class MagnetRangeUpgrade : Upgrade
    {
        protected override string Name => "Wider Magnet";
        protected override int BaseCost => 320;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.MagnetRange).AddModifier(new StatModifier(0.5f, StatModifierEffect.Add));
    }

    public class MagnetDepthUpgrade : Upgrade
    {
        protected override string Name => "Deeper Magnet";
        protected override int BaseCost => 340;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.MagnetDepth).AddModifier(new StatModifier(0.6f, StatModifierEffect.Add));
    }

    public class AutoMagnetUpgrade : Upgrade
    {
        protected override string Name => "Auto Magnet";
        protected override int BaseCost => 1800;
        protected override float CostMultiplier => 1.5f;
        protected override int MaxPurchases => 1;
        protected override void ApplyEffect() =>
            Game(GameStat.AutoMagnet).AddModifier(new StatModifier(1f, StatModifierEffect.Add));
    }

    public class AutoMagnetRateUpgrade : Upgrade
    {
        protected override string Name => "Auto Magnet Rate";
        protected override int BaseCost => 450;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.AutoMagnetInterval).AddModifier(new StatModifier(-0.9f, StatModifierEffect.Add));
    }

    #endregion

    #region Spawn

    public class FasterRainUpgrade : Upgrade
    {
        protected override string Name => "Faster Items";
        protected override int BaseCost => 150;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Game(GameStat.SpawnInterval).AddModifier(new StatModifier(-0.48f, StatModifierEffect.Add));
    }

    public class MoreItemsUpgrade : Upgrade
    {
        protected override string Name => "Max Items";
        protected override int BaseCost => 200;
        protected override float CostMultiplier => 1.65f;
        protected override int MaxPurchases => 10;
        protected override void ApplyEffect() =>
            Game(GameStat.MaxLiveItems).AddModifier(new StatModifier(6, StatModifierEffect.Add));
    }

    #endregion

    #region Money

    public class MoneyMultiplierUpgrade : Upgrade
    {
        protected override string Name => "Better Prices";
        protected override int BaseCost => 60;
        protected override float CostMultiplier => 1.55f;
        protected override int MaxPurchases => 10;
        protected override void ApplyEffect() =>
            Game(GameStat.MoneyMultiplier).AddModifier(new StatModifier(0.15f, StatModifierEffect.Add));
    }

    #endregion

    #region Item values

    public class EggValueUpgrade : Upgrade
    {
        protected override string Name => "Egg Value";
        protected override int BaseCost => 40;
        protected override float CostMultiplier => 1.5f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Item<Egg>(ItemStat.SellValue).AddModifier(new StatModifier(1f, StatModifierEffect.Add));
    }

    public class TeddyValueUpgrade : Upgrade
    {
        protected override string Name => "Teddy Value";
        protected override int BaseCost => 680;
        protected override float CostMultiplier => 1.5f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Item<TeddyBear>(ItemStat.SellValue).AddModifier(new StatModifier(20f, StatModifierEffect.Add));
    }

    public class BananaValueUpgrade : Upgrade
    {
        protected override string Name => "Banana Value";
        protected override int BaseCost => 110;
        protected override float CostMultiplier => 1.5f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Item<Banana>(ItemStat.SellValue).AddModifier(new StatModifier(3f, StatModifierEffect.Add));
    }

    public class ScrapValueUpgrade : Upgrade
    {
        protected override string Name => "Tin Can Value";
        protected override int BaseCost => 280;
        protected override float CostMultiplier => 1.45f;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Item<TinCan>(ItemStat.SellValue).AddModifier(new StatModifier(8f, StatModifierEffect.Add));
    }

    public class DiamondValueUpgrade : Upgrade
    {
        protected override string Name => "Diamond Value";
        protected override int BaseCost => 1800;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Item<Diamond>(ItemStat.SellValue).AddModifier(new StatModifier(50f, StatModifierEffect.Add));
    }

    #endregion

    #region Item weight

    public class LighterItemsUpgrade : Upgrade
    {
        protected override string Name => "Lighter Diamonds";
        protected override int BaseCost => 900;
        protected override float CostMultiplier => 1.7f;
        protected override int MaxPurchases => 4;
        protected override void ApplyEffect() =>
            Item<Diamond>(ItemStat.Mass).AddModifier(new StatModifier(-0.1f, StatModifierEffect.Add));
    }

    #endregion

    #region Item chance

    public class TinCanChanceUpgrade : ItemWeightUpgrade<TinCan>
    {
        protected override string Name => "Tin Can Luck";
        protected override int BaseCost => 350;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 4;
        protected override float WeightPerLevel => 0.12f;
    }

    public class TeddyChanceUpgrade : ItemWeightUpgrade<TeddyBear>
    {
        protected override string Name => "Teddy Luck";
        protected override int BaseCost => 900;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 4;
        protected override float WeightPerLevel => 0.1f;
    }

    public class DiamondChanceUpgrade : ItemWeightUpgrade<Diamond>
    {
        protected override string Name => "Diamond Luck";
        protected override int BaseCost => 2200;
        protected override float CostMultiplier => 1.7f;
        protected override int MaxPurchases => 4;
        protected override float WeightPerLevel => 0.08f;
    }

    #endregion

    #region Item unlocks

    public class UnlockBananaUpgrade : UnlockItemUpgrade<Banana>
    {
        protected override string Name => "Bananas";
        protected override int BaseCost => 90;
    }

    public class UnlockTinCanUpgrade : UnlockItemUpgrade<TinCan>
    {
        protected override string Name => "Tin Cans";
        protected override int BaseCost => 420;
    }

    public class UnlockTeddyUpgrade : UnlockItemUpgrade<TeddyBear>
    {
        protected override string Name => "Teddy Bears";
        protected override int BaseCost => 1300;
    }

    public class UnlockDiamondUpgrade : UnlockItemUpgrade<Diamond>
    {
        protected override string Name => "Diamonds";
        protected override int BaseCost => 3400;
    }

    #endregion

    #region Conveyor

    public class ConveyorSpeedUpgrade : Upgrade
    {
        protected override string Name => "Faster Belt";
        protected override int BaseCost => 300;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.ConveyorSpeed).AddModifier(new StatModifier(0.75f, StatModifierEffect.Add));
    }

    public class ConveyorGripUpgrade : Upgrade
    {
        protected override string Name => "Belt Grip";
        protected override int BaseCost => 250;
        protected override float CostMultiplier => 1.55f;
        protected override int MaxPurchases => 4;
        protected override void ApplyEffect() =>
            Game(GameStat.ConveyorGrip).AddModifier(new StatModifier(6f, StatModifierEffect.Add));
    }

    #endregion

    #region Object unlocks

    public class UnlockMagnetUpgrade : ActivateObjectUpgrade
    {
        protected override string Name => "Magnet";
        protected override int BaseCost => 1000;
        protected override UnlockTarget Target => UnlockTarget.Magnet;
    }

    public class UnlockConveyorUpgrade : ActivateObjectUpgrade
    {
        protected override string Name => "Conveyor Belt";
        protected override int BaseCost => 2000;
        protected override UnlockTarget Target => UnlockTarget.Conveyor;
    }

    public class UnlockAutoSeller : ActivateObjectUpgrade
    {
        protected override string Name => "AutoSeller";
        protected override int BaseCost => 2650;
        protected override UnlockTarget Target => UnlockTarget.AutoSeller;
    }

    public class UnlockResourceHoleUpgrade : ActivateObjectUpgrade
    {
        // Unlocks the whole fuel-production setup: the resource hole AND the egg converter.
        // Both scene objects share the ResourceHole unlock target, so this single upgrade
        // reveals them together (SceneRef.SetActive toggles every object under a target).
        protected override string Name => "Fuel Production";
        protected override int BaseCost => 600;
        protected override UnlockTarget Target => UnlockTarget.ResourceHole;
    }

    public class UnlockLeafBlowerUpgrade : ActivateObjectUpgrade
    {
        protected override string Name => "Leaf Blower";
        protected override int BaseCost => 1200;
        protected override UnlockTarget Target => UnlockTarget.LeafBlower;
    }

    public class UnlockSortingMachineUpgrade : ActivateObjectUpgrade
    {
        protected override string Name => "Sorting Machine";
        protected override int BaseCost => 1600;
        protected override UnlockTarget Target => UnlockTarget.SortingMachine;
    }

    #endregion

    #region Fuel production

    public class FuelPerEggUpgrade : Upgrade
    {
        protected override string Name => "Richer Eggs";
        protected override int BaseCost => 220;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.FuelPerEgg).AddModifier(new StatModifier(0.5f, StatModifierEffect.Add));
    }

    public class FuelConvertRateUpgrade : Upgrade
    {
        protected override string Name => "Faster Conversion";
        protected override int BaseCost => 300;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 6;
        // +1 egg/min per level (rate is eggs/second).
        protected override void ApplyEffect() =>
            Game(GameStat.FuelConvertRate).AddModifier(new StatModifier(1f / 60f, StatModifierEffect.Add));
    }

    #endregion

    #region Leaf blower

    public class BlowPowerUpgrade : Upgrade
    {
        protected override string Name => "Stronger Blower";
        protected override int BaseCost => 260;
        protected override float CostMultiplier => 1.55f;
        protected override int BaseFuelCost => 5;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.BlowPower).AddModifier(new StatModifier(2f, StatModifierEffect.Add));
    }

    public class BlowEfficiencyUpgrade : Upgrade
    {
        protected override string Name => "Blower Efficiency";
        protected override int BaseCost => 300;
        protected override float CostMultiplier => 1.6f;
        protected override int BaseFuelCost => 5;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Game(GameStat.BlowFuelEfficiency).AddModifier(new StatModifier(0.4f, StatModifierEffect.Add));
    }

    #endregion

    #region Sorting machine

    public class SortEfficiencyUpgrade : Upgrade
    {
        protected override string Name => "Sorter Efficiency";
        protected override int BaseCost => 320;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Game(GameStat.SortFuelEfficiency).AddModifier(new StatModifier(0.4f, StatModifierEffect.Add));
    }

    public class SortCapacityUpgrade : Upgrade
    {
        protected override string Name => "Sorter Capacity";
        protected override int BaseCost => 380;
        protected override float CostMultiplier => 1.7f;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Game(GameStat.SortCapacity).AddModifier(new StatModifier(2f, StatModifierEffect.Add));
    }

    #endregion
}