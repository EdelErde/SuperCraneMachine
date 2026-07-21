using System;

namespace CraneMachine
{
    // Base values are defined in code per item. Current values come from the
    // stat system so upgrades can modify them. Instances hold no per-instance stats.
    [Serializable]
    public abstract class ItemType
    {
        // Base values, defined in code.
        public abstract int BaseSellValue { get; }
        public abstract float BaseMass { get; }
        public virtual float SpawnWeight => 1f;

        // Stable key for this item type's stats (the concrete C# type).
        public Type Key => GetType();

        // Current (upgraded) values, resolved through the stat service.
        public int SellValue =>
            UnityEngine.Mathf.RoundToInt(ServiceLocator.StatService.ItemValue(Key, ItemStat.SellValue));
        public float Mass =>
            ServiceLocator.StatService.ItemValue(Key, ItemStat.Mass);
    }

    [Serializable]
    public class RubberDuck : ItemType
    {
        public override int BaseSellValue => 5;
        public override float BaseMass => 0.3f;
    }

    [Serializable]
    public class TeddyBear : ItemType
    {
        public override int BaseSellValue => 15;
        public override float BaseMass => 0.6f;
    }

    [Serializable]
    public class Diamond : ItemType
    {
        public override int BaseSellValue => 100;
        public override float BaseMass => 1.2f;
    }

    [Serializable]
    public class TinCan : ItemType
    {
        public override int BaseSellValue => 2;
        public override float BaseMass => 0.4f;
    }

    [Serializable]
    public class Banana : ItemType
    {
        public override int BaseSellValue => 3;
        public override float BaseMass => 0.2f;
    }
}