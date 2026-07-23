using System;
using System.Collections.Generic;
using System.Linq;
using NekoLab.Stats;
using UnityEngine;

namespace CraneMachine
{
    public class StatService : MonoBehaviour
    {
        public event Action<int> OnMoneyChanged;
        public event Action<int> OnMoneyEarned;

        private int _money;
        public int CurrentMoney => _money;
        public bool Has(int amount) => _money >= amount;

        private readonly StatContainer<GameStat> _game = new StatContainer<GameStat>();

        private readonly Dictionary<Type, StatContainer<ItemStat>> _items =
            new Dictionary<Type, StatContainer<ItemStat>>();

        private void Awake()
        {
            ServiceLocator.StatService = this;
            RegisterGameStats();
            RegisterItemStats();
        }

        private void LateUpdate()
        {
            _game.Tick();
            foreach (var c in _items.Values) c.Tick();
        }

        private void RegisterGameStats()
        {
            _game.RegisterStat(GameStat.MoneyMultiplier, 1f);
            _game.RegisterStat(GameStat.ClawSweepSpeed, 2.5f);
            _game.RegisterStat(GameStat.ClawVerticalSpeed, 3f);
            _game.RegisterStat(GameStat.GrabStrength, 100f);
            _game.RegisterStat(GameStat.SpawnInterval, 2.5f);
            _game.RegisterStat(GameStat.MaxLiveItems, 12f);
            _game.RegisterStat(GameStat.HandStrength, .3f);
            _game.RegisterStat(GameStat.DragCount, 1f);
            _game.RegisterStat(GameStat.DragRadius, 0.75f);
        }

        private void RegisterItemStats()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => typeof(ItemType).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var t in types)
            {
                var proto = (ItemType)Activator.CreateInstance(t);
                var container = new StatContainer<ItemStat>();
                container.RegisterStat(ItemStat.SellValue, proto.BaseSellValue);
                container.RegisterStat(ItemStat.Mass, proto.BaseMass);
                container.RegisterStat(ItemStat.Unlocked, proto.StartsUnlocked ? 1f : 0f);
                _items[t] = container;
            }
        }

        public Stat Game(GameStat stat) => _game.Get(stat);
        public float GameValue(GameStat stat) => _game.Get(stat).Value;

        public Stat ItemStatOf(Type itemType, ItemStat prop) => _items[itemType].Get(prop);
        public float ItemValue(Type itemType, ItemStat prop) => _items[itemType].Get(prop).Value;

        public void AddMoney(int amount)
        {
            if (amount <= 0) return;
            _money += amount;
            OnMoneyEarned?.Invoke(amount);
            OnMoneyChanged?.Invoke(_money);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (!Has(amount)) return false;
            _money -= amount;
            OnMoneyChanged?.Invoke(_money);
            return true;
        }
    }
}