using System;
using NekoLab.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CraneMachine
{
    [DefaultExecutionOrder(-100)]
    public class FuelService : MonoBehaviour
    {
        public event Action<float> OnFuelChanged;
        public event Action<float> OnFuelGained;

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

        public bool TrySpend(float amount)
        {
            if (amount <= 0f) return true;
            if (!Has(amount)) return false;
            SetTo(CurrentFuel - amount);
            return true;
        }

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
            stat.Value = Mathf.Max(0f, value);
            OnFuelChanged?.Invoke(CurrentFuel);
        }
    }
}