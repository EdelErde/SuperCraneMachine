using UnityEngine;

namespace CraneMachine
{
    // The Fuel Funnel. Final step of the physical fuel pipeline: player drags a Fuel
    // item (produced by a FuelFilter) in here, it's consumed, and its value feeds the
    // shared fuel pool via FuelService. Only accepts the Fuel item type — other items
    // passing over do nothing (left for other machines/holes).
    //
    // Polls overlaps each physics step rather than relying on OnTriggerStay2D, same
    // reasoning as FuelHole/ResourceHole: a settled/sleeping item stops receiving Stay
    // callbacks, which would let it sit in the funnel forever.
    [RequireComponent(typeof(Collider2D))]
    public class FuelFunnel : MonoBehaviour
    {
        [Tooltip("How much fuel one Fuel item is worth when funneled in.")]
        [SerializeField] private float fuelPerItem = 5f;

        [Tooltip("Layers that items live on (leave as Everything if unsure).")]
        [SerializeField] private LayerMask itemLayers = ~0;

        // SFX lives in the dedicated SFX/ components (see FuelFunnelSfx), which listen
        // to this event rather than the funnel owning sound config itself.
        public event System.Action OnFunneled;

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
                if (item.type == null || !(item.type is Fuel)) continue;

                Consume(item);
            }
        }

        private void Consume(Item item)
        {
            if (ServiceLocator.FuelService != null)
                ServiceLocator.FuelService.Add(fuelPerItem);

            OnFunneled?.Invoke();

            Destroy(item.gameObject);
        }
    }
}