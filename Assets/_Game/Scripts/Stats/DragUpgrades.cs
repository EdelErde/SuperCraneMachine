using NekoLab.Stats;

namespace CraneMachine
{
    public class HandStrengthUpgrade : Upgrade
    {
        protected override string Name => "Stronger Hands";
        protected override int BaseCost => 20;
        protected override int MaxPurchases => 8;

        protected override void ApplyEffect() =>
            Game(GameStat.HandStrength).AddModifier(new StatModifier(.25f, StatModifierEffect.Add));
    }

    public class DragCountUpgrade : Upgrade
    {
        protected override string Name => "Extra Hand";
        protected override int BaseCost => 100;
        protected override float CostMultiplier => 2f;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Game(GameStat.DragCount).AddModifier(new StatModifier(1f, StatModifierEffect.Add));
    }

    public class DragRadiusUpgrade : Upgrade
    {
        protected override string Name => "Wider Reach";
        protected override int BaseCost => 40;
        protected override void ApplyEffect() =>
            Game(GameStat.DragRadius).AddModifier(new StatModifier(0.25f, StatModifierEffect.Add));
    }
}