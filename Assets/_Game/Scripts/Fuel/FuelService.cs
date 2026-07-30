using System;
using NekoLab.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CraneMachine
{
    // Central store for the shared, uncapped fuel pool.
    // Fuel is kept inside the stat system (GameStat.Fuel) so upgrades and other
    // systems can read it uniformly, but spend/gain go through here so the HUD and
    // machines get a single change event and a single set of rules.
    public class FuelService : MonoBehaviour
    {
        public event Action<float> OnFuelChanged;   // new total
        public event Action<float> OnFuelGained;    // amount added

        private void Awake() => ServiceLocator.FuelService = this;

        private Stat FuelStat => ServiceLocator.StatService.Game(GameStat.Fuel);

        public float CurrentFuel =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.Fuel) : 0f;

        public bool Has(float amount) => CurrentFuel >= amount;

        [Button]
        public void Add(float amount)
        {
            if (amount <= 0f) return;
            SetTo(CurrentFuel + amount);
            OnFuelGained?.Invoke(amount);
        }

        // Try to spend a fixed amount (all or nothing).
        public bool TrySpend(float amount)
        {
            if (amount <= 0f) return true;
            if (!Has(amount)) return false;
            SetTo(CurrentFuel - amount);
            return true;
        }

        // Spend as much as is available up to 'amount'; returns what was actually spent.
        // Used by machines that drain continuously and should sputter out gracefully.
        public float SpendUpTo(float amount)
        {
            if (amount <= 0f) return 0f;
            float spent = Mathf.Min(amount, CurrentFuel);
            if (spent <= 0f) return 0f;
            SetTo(CurrentFuel - spent);
            return spent;
        }

        private void SetTo(float value)
        {
            var stat = FuelStat;
            if (stat == null) return;
            // Stat.Value setter shifts the offset so the final value becomes 'value'.
            stat.Value = Mathf.Max(0f, value);
            OnFuelChanged?.Invoke(CurrentFuel);
        }
    }
}
