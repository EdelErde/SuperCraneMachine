using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    [DefaultExecutionOrder(-100)]
    public class UpgradeService : MonoBehaviour
    {
        private readonly Dictionary<Type, int> _purchases = new Dictionary<Type, int>();
        private readonly Dictionary<Type, UpgradeButton> _buttons = new Dictionary<Type, UpgradeButton>();

        public event Action OnUpgradesChanged;

        public void RegisterButton(UpgradeButton button)
        {
            if (button != null && button.Upgrade != null)
                _buttons[button.Upgrade.GetType()] = button;
        }

        public void UnregisterButton(UpgradeButton button)
        {
            if (button != null && button.Upgrade != null)
                _buttons.Remove(button.Upgrade.GetType());
        }

        public UpgradeButton FindButton(Type upgradeType)
            => _buttons.TryGetValue(upgradeType, out var b) ? b : null;

        // All upgrades currently registered via their buttons (for telemetry/export).
        public IEnumerable<IUpgrade> AllUpgrades()
        {
            foreach (var b in _buttons.Values)
                if (b != null && b.Upgrade != null)
                    yield return b.Upgrade;
        }

        private void Awake() => ServiceLocator.UpgradeService = this;

        // Broadcast that the set of upgrades/buttons may have changed. Views call this after
        // they finish registering their buttons, so cross-button gate/preview relationships
        // resolve without waiting a frame.
        public void NotifyChanged() => OnUpgradesChanged?.Invoke();

        public bool CanAfford(IUpgrade upgrade)
        {
            if (upgrade.MaxedOut) return false;
            if (!ServiceLocator.StatService.Has(upgrade.CurrentCost)) return false;
            if (upgrade.CurrentFuelCost > 0)
            {
                var fuel = ServiceLocator.FuelService;
                if (fuel == null || !fuel.Has(upgrade.CurrentFuelCost)) return false;
            }
            return true;
        }

        public bool TryBuy(IUpgrade upgrade)
        {
            if (upgrade.MaxedOut) return false;

            int fuelCost = upgrade.CurrentFuelCost;
            var fuel = ServiceLocator.FuelService;

            // Check fuel availability BEFORE spending money, so we never take money and then
            // fail on fuel.
            if (fuelCost > 0 && (fuel == null || !fuel.Has(fuelCost))) return false;

            if (!ServiceLocator.StatService.TrySpend(upgrade.CurrentCost)) return false;

            // Money is spent; fuel was pre-checked, so this should succeed. Guard anyway.
            if (fuelCost > 0 && !fuel.TrySpend(fuelCost))
            {
                // Refund money if fuel somehow failed (e.g. drained same frame).
                ServiceLocator.StatService.AddMoney(upgrade.CurrentCost);
                return false;
            }

            upgrade.Apply();
            _purchases.TryGetValue(upgrade.GetType(), out var n);
            _purchases[upgrade.GetType()] = n + 1;

            OnUpgradesChanged?.Invoke();
            return true;
        }

        public int TimesPurchased(Type upgradeType)
            => _purchases.TryGetValue(upgradeType, out var n) ? n : 0;

        public bool IsPurchased(Type upgradeType) => TimesPurchased(upgradeType) > 0;

        // Total upgrade purchases across every upgrade (used for page-unlock gating).
        public int TotalPurchases()
        {
            int total = 0;
            foreach (var n in _purchases.Values) total += n;
            return total;
        }
    }
}