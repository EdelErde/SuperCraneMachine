using System;

namespace CraneMachine
{
    [Serializable]
    public abstract class ItemType
    {
        public abstract int BaseSellValue { get; }
        public abstract float BaseMass { get; }
        public virtual float SpawnWeight => 1f;

        public virtual bool StartsUnlocked => true;

        // Whether drones can carry this type from the start (before any drone-carry upgrade).
        // Only Fuel is haulable out of the gate; every other type is unlocked for drone
        // hauling by an UnlockDroneCarryUpgrade. Defaults to false so new types stay gated
        // unless they opt in.
        public virtual bool StartsDroneCarryable => false;

        public virtual string DisplayName => GetType().Name;

        public Type Key => GetType();

        public int SellValue =>
            UnityEngine.Mathf.RoundToInt(ServiceLocator.StatService.ItemValue(Key, ItemStat.SellValue));
        public float Mass =>
            ServiceLocator.StatService.ItemValue(Key, ItemStat.Mass);

        public bool Unlocked =>
            ServiceLocator.StatService == null ||
            ServiceLocator.StatService.ItemValue(Key, ItemStat.Unlocked) > 0f;

        public float CurrentWeight =>
            ServiceLocator.StatService == null
                ? SpawnWeight
                : UnityEngine.Mathf.Max(0f, ServiceLocator.StatService.ItemValue(Key, ItemStat.Weight));
    }

    [Serializable]
    public class Egg : ItemType
    {
        public override int BaseSellValue => 5;
        public override float BaseMass => 0.25f;
        public override float SpawnWeight => 1.0f;
    }

    [Serializable]
    public class Banana : ItemType
    {
        public override int BaseSellValue => 14;
        public override float BaseMass => 0.3f;
        public override float SpawnWeight => 0.85f;
        public override bool StartsUnlocked => false;
    }

    [Serializable]
    public class TinCan : ItemType
    {
        public override int BaseSellValue => 35;
        public override float BaseMass => 0.4f;
        public override float SpawnWeight => 0.65f;
        public override bool StartsUnlocked => false;
    }

    [Serializable]
    public class TeddyBear : ItemType
    {
        public override int BaseSellValue => 85;
        public override float BaseMass => 0.55f;
        public override float SpawnWeight => 0.45f;
        public override bool StartsUnlocked => false;
    }

    [Serializable]
    public class Diamond : ItemType
    {
        public override int BaseSellValue => 220;
        public override float BaseMass => 1.2f;
        public override float SpawnWeight => 0.28f;
        public override bool StartsUnlocked => false;
    }

    // Produced only by the Fuel Filter (never rains from the spawner — SpawnWeight is
    // always 0 regardless of upgrades). Carried by hand or by drone into the Fuel
    // Funnel, which converts it into the shared fuel pool.
    [Serializable]
    public class Fuel : ItemType
    {
        public override int BaseSellValue => 0;
        public override float BaseMass => 0.2f;
        public override float SpawnWeight => 0f;

        // The first (and default) thing drones can haul. Everything else needs an upgrade.
        public override bool StartsDroneCarryable => true;
    }
}