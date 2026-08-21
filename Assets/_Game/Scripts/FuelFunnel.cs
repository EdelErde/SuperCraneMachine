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
        [Tooltip("Base fuel one Fuel item is worth when funneled in, BEFORE the FuelPerEgg " +
                 "stat multiplier. Kept low so the early economy is weak; the 'Richer Eggs' " +
                 "upgrades scale it up over the run.")]
        [SerializeField] private float fuelPerItem = 2f;

        // Live yield = base × FuelPerEgg stat. FuelPerEgg starts below 1 (see StatService)
        // so early fuel items are worth well under the old flat 5, then the Richer Eggs /
        // Enriched Eggs upgrades push the multiplier up as the player progresses. Floored
        // so a mis-set stat can't zero out production.
        private float FuelPerItem =>
            ServiceLocator.StatService != null
                ? fuelPerItem * Mathf.Max(0.05f, ServiceLocator.StatService.GameValue(GameStat.FuelPerEgg))
                : fuelPerItem;

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
                ServiceLocator.FuelService.Add(FuelPerItem);

            OnFunneled?.Invoke();

            Destroy(item.gameObject);
        }
    }
}