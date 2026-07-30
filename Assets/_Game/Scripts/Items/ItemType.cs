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
}