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

        public Type Key => GetType();

        public int SellValue =>
            UnityEngine.Mathf.RoundToInt(ServiceLocator.StatService.ItemValue(Key, ItemStat.SellValue));
        public float Mass =>
            ServiceLocator.StatService.ItemValue(Key, ItemStat.Mass);

        public bool Unlocked =>
            ServiceLocator.StatService == null ||
            ServiceLocator.StatService.ItemValue(Key, ItemStat.Unlocked) > 0f;
    }

    [Serializable]
    public class Egg : ItemType
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
        public override bool StartsUnlocked => false;
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