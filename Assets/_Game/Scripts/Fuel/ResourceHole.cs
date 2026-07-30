using UnityEngine;

namespace CraneMachine
{
    // The "second hole" from the mockup. Items dropped here are not sold; they are
    // handed to a ResourceConverter which slowly turns them into a resource (fuel).
    // Mirrors SellHole's trigger pattern.
    [RequireComponent(typeof(Collider2D))]
    public class ResourceHole : MonoBehaviour
    {
        [Tooltip("Converter that receives items dropped in this hole.")]
        [SerializeField] private ResourceConverter converter;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Awake()
        {
            if (converter == null)
                converter = GetComponentInParent<ResourceConverter>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item == null) return;

            if (converter != null)
                converter.Accept(item);
            else
                Destroy(item.gameObject); // no converter wired: consume so it doesn't linger
        }
    }
}