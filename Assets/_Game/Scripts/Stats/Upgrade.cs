using System;
using NekoLab.Stats;
using UnityEngine;

namespace CraneMachine
{
    [Serializable]
    public abstract class Upgrade : IUpgrade
    {
        protected abstract string Name { get; }
        protected abstract int BaseCost { get; }
        protected virtual float CostMultiplier => 1.5f;
        protected virtual int MaxPurchases => 0; 
        int IUpgrade.MaxPurchases => MaxPurchases;
        public int MaxPurchasesValue => MaxPurchases;
        protected virtual string Icon => null;

        public string DisplayName => Name;
        public string IconPath => Icon;

        protected abstract void ApplyEffect();

        private int _timesPurchased;
        public int TimesPurchased => _timesPurchased;

        public bool MaxedOut => MaxPurchases > 0 && _timesPurchased >= MaxPurchases;
        public int CurrentCost => Mathf.RoundToInt(BaseCost * Mathf.Pow(CostMultiplier, _timesPurchased));
        public string Label => MaxedOut ? $"{Name} (MAX)" : $"{Name} Lv.{_timesPurchased}";

        public void Apply()
        {
            ApplyEffect();
            _timesPurchased++;
        }

        protected static Stat Game(GameStat s) => ServiceLocator.StatService.Game(s);
        protected static Stat Item<T>(ItemStat prop) where T : ItemType
            => ServiceLocator.StatService.ItemStatOf(typeof(T), prop);
    }
}