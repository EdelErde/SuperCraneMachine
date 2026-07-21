using NekoLab.Stats;

namespace CraneMachine
{

    public class MoneyMultiplierUpgrade : Upgrade
    {
        protected override string Name => "Better Prices";
        protected override int BaseCost => 25;
        protected override void ApplyEffect() =>
            Game(GameStat.MoneyMultiplier).AddModifier(new StatModifier(0.1f, StatModifierEffect.Add));
    }

    public class ClawSpeedUpgrade : Upgrade
    {
        protected override string Name => "Faster Claw";
        protected override int BaseCost => 15;
        protected override void ApplyEffect() =>
            Game(GameStat.ClawSweepSpeed).AddModifier(new StatModifier(0.5f, StatModifierEffect.Add));
    }

    public class GrabStrengthUpgrade : Upgrade
    {
        protected override string Name => "Stronger Grip";
        protected override int BaseCost => 20;
        protected override void ApplyEffect() =>
            Game(GameStat.GrabStrength).AddModifier(new StatModifier(25f, StatModifierEffect.Add));
    }

    public class FasterRainUpgrade : Upgrade
    {
        protected override string Name => "More Items";
        protected override int BaseCost => 20;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Game(GameStat.SpawnInterval).AddModifier(new StatModifier(-0.05f, StatModifierEffect.Add));
    }
    
    public class DiamondValueUpgrade : Upgrade
    {
        protected override string Name => "Diamond Polish";
        protected override int BaseCost => 100;
        protected override void ApplyEffect() =>
            Item<Diamond>(ItemStat.SellValue).AddModifier(new StatModifier(50f, StatModifierEffect.Add));
    }
}