using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class UpgradeService : MonoBehaviour
    {
        private readonly Dictionary<Type, int> _purchases = new Dictionary<Type, int>();

        public event Action OnUpgradesChanged;

        private void Awake() => ServiceLocator.UpgradeService = this;

        public bool CanAfford(IUpgrade upgrade)
            => !upgrade.MaxedOut && ServiceLocator.StatService.Has(upgrade.CurrentCost);

        public bool TryBuy(IUpgrade upgrade)
        {
            if (upgrade.MaxedOut) return false;
            if (!ServiceLocator.StatService.TrySpend(upgrade.CurrentCost)) return false;

            upgrade.Apply();
            _purchases.TryGetValue(upgrade.GetType(), out var n);
            _purchases[upgrade.GetType()] = n + 1;

            OnUpgradesChanged?.Invoke();
            return true;
        }

        public int TimesPurchased(Type upgradeType)
            => _purchases.TryGetValue(upgradeType, out var n) ? n : 0;

        public bool IsPurchased(Type upgradeType) => TimesPurchased(upgradeType) > 0;
    }
}