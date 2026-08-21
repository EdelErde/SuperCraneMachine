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
        protected override int BaseCost => 5760;         // rebalanced: drone-tier gate x2.4
        protected override UnlockTarget Target => UnlockTarget.DroneFab;
    }

    #endregion

    #region Drone carry unlocks (which item types drones may haul)

    // Flips a single item type's DroneCarry stat on, so drones are allowed to pick it up.
    // Mirrors UnlockItemUpgrade<T> (which flips ItemStat.Unlocked) exactly — same one-shot
    // shape, just a different stat. Fuel is carryable from the start (Fuel.StartsDroneCarryable
    // == true) so it needs no unlock here; every other haulable type gets one of these.
    public abstract class UnlockDroneCarryUpgrade<T> : Upgrade where T : ItemType
    {
        protected override int MaxPurchases => 1;
        protected override void ApplyEffect() =>
            Item<T>(ItemStat.DroneCarry).AddModifier(new StatModifier(1f, StatModifierEffect.Add));
    }

    public class DroneCarryEggUpgrade : UnlockDroneCarryUpgrade<Egg>
    {
        protected override string Name => "Drone Hauling: Eggs";
        protected override int BaseCost => 1680;        // rebalanced: drone-tier x2.4
    }

    public class DroneCarryBananaUpgrade : UnlockDroneCarryUpgrade<Banana>
    {
        protected override string Name => "Drone Hauling: Bananas";
        protected override int BaseCost => 2640;        // rebalanced: drone-tier x2.4
    }

    public class DroneCarryTinCanUpgrade : UnlockDroneCarryUpgrade<TinCan>
    {
        protected override string Name => "Drone Hauling: Tin Cans";
        protected override int BaseCost => 3840;        // rebalanced: drone-tier x2.4
    }

    public class DroneCarryTeddyUpgrade : UnlockDroneCarryUpgrade<TeddyBear>
    {
        protected override string Name => "Drone Hauling: Teddy Bears";
        protected override int BaseCost => 5280;        // rebalanced: drone-tier x2.4
    }

    public class DroneCarryDiamondUpgrade : UnlockDroneCarryUpgrade<Diamond>
    {
        protected override string Name => "Drone Hauling: Diamonds";
        protected override int BaseCost => 7680;        // rebalanced: drone-tier x2.4
    }

    #endregion

    #region Drone fab / drone tuning

    // Lowers production time (seconds per drone) — negative Add, like FuelFilterSpeed.
    // DroneFab floors the stat at 0.1s so it can never hit zero.
    public class DroneProductionTimeUpgrade : Upgrade
    {
        protected override string Name => "Faster Fab";
        protected override int BaseCost => 1200;         // rebalanced: drone-tier x2.4
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.DroneProductionTime).AddModifier(new StatModifier(-0.5f, StatModifierEffect.Add));
    }

    // +2 deliveries per drone per level. Higher = drones live longer before dying.
    public class DroneChargesUpgrade : Upgrade
    {
        protected override string Name => "Drone Endurance";
        protected override int BaseCost => 1560;         // rebalanced: drone-tier x2.4
        protected override float CostMultiplier => 1.7f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.DroneCharges).AddModifier(new StatModifier(2f, StatModifierEffect.Add));
    }

    // Faster empty travel.
    public class DroneSpeedUpgrade : Upgrade
    {
        protected override string Name => "Faster Drones";
        protected override int BaseCost => 1010;         // rebalanced: drone-tier x2.4
        protected override float CostMultiplier => 1.55f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.DroneSpeed).AddModifier(new StatModifier(0.2f, StatModifierEffect.Add));
    }

    // Faster carrying (closes the gap between empty and loaded speed).
    public class DroneCarrySpeedUpgrade : Upgrade
    {
        protected override string Name => "Stronger Rotors";
        protected override int BaseCost => 1150;         // rebalanced: drone-tier x2.4
        protected override float CostMultiplier => 1.6f;
        protected override int MaxPurchases => 6;
        protected override void ApplyEffect() =>
            Game(GameStat.DroneCarrySpeed).AddModifier(new StatModifier(0.15f, StatModifierEffect.Add));
    }

    #endregion

    #region Fuel economy (relational — extend existing Fuel stats for the drone era)

    // The starting fuel economy is exactly one droplet per egg (FuelPerEgg base 1.0 ×
    // FuelFunnel base 1). "Richer Eggs" (+0.5/lvl) climbs from there; this is a SECOND,
    // later tier for when a drone-fed factory needs far more fuel throughput. Same stat as
    // FuelPerEggUpgrade, so it's fully live — it just stacks bigger, gated behind the fab.
    public class RicherEggsIIUpgrade : Upgrade
    {
        protected override string Name => "Enriched Eggs";
        protected override int BaseCost => 3360;         // rebalanced: drone-tier x2.4
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
        protected override int BaseCost => 2160;         // rebalanced: drone-tier x2.4
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