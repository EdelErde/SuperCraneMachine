using UnityEngine;

namespace CraneMachine
{
    // The "second hole" from the mockup. Items dropped here are not sold; they are handed to
    // a ResourceConverter which slowly turns them into a resource (fuel).
    //
    // We POLL overlaps each physics step instead of relying on OnTriggerStay2D: a settled item
    // sleeps, and Unity stops sending Stay callbacks to sleeping bodies, which would let an
    // item dropped in the hole sit there forever. Polling is immune to sleep state. We still
    // skip items the player is actively dragging, so dragging over the hole never yanks an
    // item out of their hand (and never destroys a held item mid-drag).
    [RequireComponent(typeof(Collider2D))]
    public class ResourceHole : MonoBehaviour
    {
        [Tooltip("Converter that receives items dropped in this hole.")]
        [SerializeField] private ResourceConverter converter;

        [Tooltip("Layers that items live on (leave as Everything if unsure).")]
        [SerializeField] private LayerMask itemLayers = ~0;

        private Collider2D _col;
        private readonly Collider2D[] _overlap = new Collider2D[16];
        private ContactFilter2D _filter;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            if (converter == null)
                converter = GetComponentInParent<ResourceConverter>();

            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = itemLayers,
                useTriggers = true,
            };
        }

        private void FixedUpdate()
        {
            if (_col == null) return;

            int count = _col.Overlap(_filter, _overlap);
            for (int i = 0; i < count; i++)
            {
                var col = _overlap[i];
                if (col == null) continue;

                var item = col.GetComponentInParent<Item>();
                if (item == null) continue;

                if (item.IsDragging) continue;

                Consume(item);
            }
        }

        private void Consume(Item item)
        {
            if (converter != null)
                converter.Accept(item);
            else
                Destroy(item.gameObject);
        }
    }
}