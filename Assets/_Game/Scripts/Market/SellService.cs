using System;
using UnityEngine;

namespace CraneMachine
{
    public class SellService : MonoBehaviour
    {
        public event Action<int, Vector3> OnItemSold;

        private void Awake() => ServiceLocator.SellService = this;

        public int Sell(Item item)
        {
            if (item == null || item.type == null) return 0;

            float mult = ServiceLocator.StatService.GameValue(GameStat.MoneyMultiplier);
            int payout = Mathf.RoundToInt(item.SellValue * mult);

            Vector3 pos = item.transform.position;
            ServiceLocator.StatService.AddMoney(payout);
            OnItemSold?.Invoke(payout, pos);

            Destroy(item.gameObject);
            return payout;
        }
    }
}