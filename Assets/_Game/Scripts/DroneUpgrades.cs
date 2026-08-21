using NekoLab.Stats;

namespace CraneMachine
{
    // Drone upgrades. Same shape as the existing Upgrade classes in Upgrades.cs — drop
    // these in there (or keep them here; the UpgradeService discovers them via their
    // buttons either way). They depend on the five new GameStat entries listed in
    // DroneStats.cs; add those to the GameStat enum + register them in StatService first.

    #region Drone fab unlock

    public class UnlockDroneFabUpgrade : ActivateObjectUpgrade
    {
        protected override string Name => "Drone Fab";
        protected override int BaseCost => 2400;
        protected override UnlockTarget Target => UnlockTarget.DroneFab;
    }

    #endregion

    #region Drone fab / drone tuning

    // Lowers production time (seconds per drone) — negative Add, like FuelFilterSpeed.
    // DroneFab floors the stat at 0.1s so it can never hit zero.
    public class DroneProductionTimeUpgrade : Upgrade
    {
        protected override string Name => "Faster Fab";
        protected override int BaseCost => 500;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.DroneProductionTime).AddModifier(new StatModifier(-0.5f, StatModifierEffect.Add));
    }

    // +2 deliveries per drone per level. Higher = drones live longer before dying.
    public class DroneChargesUpgrade : Upgrade
    {
        protected override string Name => "Drone Endurance";
        protected override int BaseCost => 650;
        protected override float CostMultiplier => 1.7f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.DroneCharges).AddModifier(new StatModifier(2f, StatModifierEffect.Add));
    }

    // Faster empty travel.
    public class DroneSpeedUpgrade : Upgrade
    {
        protected override string Name => "Faster Drones";
        protected override int BaseCost => 420;
        protected override float CostMultiplier => 1.55f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.DroneSpeed).AddModifier(new StatModifier(0.2f, StatModifierEffect.Add));
    }

    // Faster carrying (closes the gap between empty and loaded speed).
    public class DroneCarrySpeedUpgrade : Upgrade
    {
        protected override string Name => "Stronger Rotors";
        protected override int BaseCost => 480;
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.DroneCarrySpeed).AddModifier(new StatModifier(0.15f, StatModifierEffect.Add));
    }

    #endregion

    #region Fuel economy (relational — extend existing Fuel stats for the drone era)

    // The starting fuel economy is deliberately sluggish (FuelPerEgg base lowered to 0.5,
    // FuelConvertRate to 1/60). "Richer Eggs" climbs out of that; this is a SECOND, later
    // tier for when a drone-fed factory needs far more fuel throughput. Same stat as
    // FuelPerEggUpgrade, so it's fully live — it just stacks bigger, gated behind the fab.
    public class RicherEggsIIUpgrade : Upgrade
    {
        protected override string Name => "Enriched Eggs";
        protected override int BaseCost => 1400;
        protected override float CostMultiplier => 1.7f;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect() =>
            Game(GameStat.FuelPerEgg).AddModifier(new StatModifier(1f, StatModifierEffect.Add));
    }

    // Running the blower AND sorter AND feeding a drone line burns fuel fast. This tightens
    // the whole factory's fuel drain at once by improving both efficiency multipliers, so a
    // single buy relieves fuel pressure across every fuel-consuming machine. Reuses the
    // existing BlowFuelEfficiency + SortFuelEfficiency stats (both live).
    public class FuelReserveUpgrade : Upgrade
    {
        protected override string Name => "Fuel Reserves";
        protected override int BaseCost => 900;
        protected override float CostMultiplier => 1.65f;
        protected override int MaxPurchases => 5;
        protected override void ApplyEffect()
        {
            Game(GameStat.BlowFuelEfficiency).AddModifier(new StatModifier(0.25f, StatModifierEffect.Add));
            Game(GameStat.SortFuelEfficiency).AddModifier(new StatModifier(0.25f, StatModifierEffect.Add));
        }
    }

    #endregion
}