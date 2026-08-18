using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(Collider2D))]
    public class SellHole : MonoBehaviour
    {
        // For SfxManager — fires the moment an item drops into the hole, distinct from
        // SellService.OnItemSold (which fires once the sale is actually processed).
        public event System.Action OnItemEntered;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item == null) return;

            OnItemEntered?.Invoke();

            if (ServiceLocator.SellService != null)
                ServiceLocator.SellService.Sell(item);
        }
    }
}