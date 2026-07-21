using UnityEngine;

namespace CraneMachine
{
    public class SellService : MonoBehaviour
    {
        private void Awake() => ServiceLocator.SellService = this;

        public int Sell(Item item)
        {
            if (item == null || item.type == null) return 0;

            float mult = ServiceLocator.StatService.GameValue(GameStat.MoneyMultiplier);
            int payout = Mathf.RoundToInt(item.SellValue * mult);

            ServiceLocator.StatService.AddMoney(payout);
            Destroy(item.gameObject);
            return payout;
        }
    }
}