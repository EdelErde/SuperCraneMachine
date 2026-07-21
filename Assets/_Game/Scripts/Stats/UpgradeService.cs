using UnityEngine;

namespace CraneMachine
{
    public class UpgradeService : MonoBehaviour
    {
        private void Awake() => ServiceLocator.UpgradeService = this;

        public bool CanAfford(IUpgrade upgrade)
            => !upgrade.MaxedOut && ServiceLocator.StatService.Has(upgrade.CurrentCost);

        public bool TryBuy(IUpgrade upgrade)
        {
            if (upgrade.MaxedOut) return false;
            if (!ServiceLocator.StatService.TrySpend(upgrade.CurrentCost)) return false;
            upgrade.Apply();
            return true;
        }
    }
}