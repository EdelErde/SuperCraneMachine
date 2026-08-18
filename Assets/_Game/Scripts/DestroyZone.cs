using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(Collider2D))]
    public class DestroyZone : MonoBehaviour
    {
        // For SfxManager.
        public event System.Action OnItemDestroyed;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item != null)
            {
                OnItemDestroyed?.Invoke();
                Destroy(item.gameObject);
            }
        }
    }
}