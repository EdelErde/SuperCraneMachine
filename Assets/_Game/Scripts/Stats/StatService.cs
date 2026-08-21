using System;
using System.Collections.Generic;
using System.Linq;
using NekoLab.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CraneMachine
{
    // Services initialize before views/buttons so ServiceLocator is populated in time.
    [DefaultExecutionOrder(-100)]
    public class StatService : MonoBehaviour
    {
        public event Action<int> OnMoneyChanged;
        public event Action<int> OnMoneyEarned;

        private int _money;
        public int CurrentMoney => _money;
        public bool Has(int amount) => _money >= amount;

        // Lifetime running totals — only ever increase, unlike CurrentMoney/Fuel which
        // can drop when spent. Intended for unlock conditions ("reach X total money
        // earned") where a spendable balance would be an unstable trigger.
        private int _totalMoneyEarned;
        private float _totalFuelProduced;
        public int TotalMoneyEarned => _totalMoneyEarned;
        public float TotalFuelProduced => _totalFuelProduced;

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
            _game.RegisterStat(GameStat.MagnetSweepSpeed, 2.5f);
            _game.RegisterStat(GameStat.MagnetVerticalSpeed, 3f);
            _game.RegisterStat(GameStat.MagnetGrabCapacity, 2f);
            _game.RegisterStat(GameStat.MagnetRange, 1f);
            _game.RegisterStat(GameStat.MagnetDepth, .5f);
            _game.RegisterStat(GameStat.SpawnInterval, 2.5f);
            _game.RegisterStat(GameStat.MaxLiveItems, 4f);
            _game.RegisterStat(GameStat.HandStrength, .3f);
            _game.RegisterStat(GameStat.DragCount, 1f);
            _game.RegisterStat(GameStat.DragRadius, 0.75f);
            _game.RegisterStat(GameStat.AutoMagnet, 0f);
            _game.RegisterStat(GameStat.AutoMagnetInterval, 8f);
            _game.RegisterStat(GameStat.ConveyorSpeed, 2f);
            _game.RegisterStat(GameStat.ConveyorGrip, 12f);

            _game.RegisterStat(GameStat.Fuel, 0f);
            // Live multiplier on FuelFunnel yield (base 2 × this). Starts weak (0.6 → ~1.2
            // fuel per item vs the old flat 5) so early fuel is scarce; the Richer Eggs /
            // Enriched Eggs upgrades raise it over the run.
            _game.RegisterStat(GameStat.FuelPerEgg, 1f);   // 1 egg -> 1 fuel droplet at start (× fuelPerItem base 1)
            _game.RegisterStat(GameStat.FuelConvertRate, 1f / 30f); // unused by current pipeline; left at original

            // Leaf blower
            _game.RegisterStat(GameStat.BlowPower, 6f);
            _game.RegisterStat(GameStat.BlowFuelEfficiency, 1f);

            // Sorting machine
            _game.RegisterStat(GameStat.SortFuelEfficiency, 1f);
            _game.RegisterStat(GameStat.SortCapacity, 4f);

            // Fuel filter
            _game.RegisterStat(GameStat.FuelFilterProcessTime, 1.5f);
            _game.RegisterStat(GameStat.FuelFilterCapacity, 4f);

            // Drone fab
            _game.RegisterStat(GameStat.DroneProductionTime, 4f);
            _game.RegisterStat(GameStat.DroneCharges, 5f);
            _game.RegisterStat(GameStat.DroneSpeed, 1f);
            _game.RegisterStat(GameStat.DroneCarrySpeed, 0.8f);
            _game.RegisterStat(GameStat.DroneGrip, 1f);
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
                container.RegisterStat(ItemStat.Weight, proto.SpawnWeight);
                _items[t] = container;
            }
        }

        public Stat Game(GameStat stat) => _game.Get(stat);
        public float GameValue(GameStat stat) => _game.Get(stat).Value;

        public Stat ItemStatOf(Type itemType, ItemStat prop) => _items[itemType].Get(prop);
        public float ItemValue(Type itemType, ItemStat prop) => _items[itemType].Get(prop).Value;

        [Button]
        public void AddMoney(int amount)
        {
            if (amount <= 0) return;
            _money += amount;
            _totalMoneyEarned += amount;
            OnMoneyEarned?.Invoke(amount);
            OnMoneyChanged?.Invoke(_money);
        }

        // Called by FuelService whenever fuel is added to the pool, so the lifetime
        // total tracks production regardless of how much is later spent on upgrades.
        public void NotifyFuelProduced(float amount)
        {
            if (amount > 0f) _totalFuelProduced += amount;
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