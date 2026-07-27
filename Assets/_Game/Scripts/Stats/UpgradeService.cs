using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
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

        private void Awake() => ServiceLocator.UpgradeService = this;

        private void Start() => StartCoroutine(SyncAfterRegistration());

        private System.Collections.IEnumerator SyncAfterRegistration()
        {
            yield return new WaitForEndOfFrame();
            OnUpgradesChanged?.Invoke();
        }

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