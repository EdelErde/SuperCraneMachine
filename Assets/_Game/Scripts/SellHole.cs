using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(Collider2D))]
    public class SellHole : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item == null) return;

            if (ServiceLocator.SellService != null)
                ServiceLocator.SellService.Sell(item);
        }
    }
}