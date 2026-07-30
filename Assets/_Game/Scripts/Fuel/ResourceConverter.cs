using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // The "Production Window" from the mockup: eggs are dropped in and slowly turned
    // into fuel at a configurable rate (mockup shows "1/min", batch "5x").
    //
    // KISS: one converter, one recipe (Egg -> Fuel) driven by stats, so upgrades tune it.
    // Open for extension: swap/add recipes by implementing IResourceRecipe.
    public class ResourceConverter : MonoBehaviour
    {
        [Tooltip("Which item type this converter accepts. Others are ignored (left for other holes).")]
        [SerializeReference] private ItemType accepts = new Egg();

        [Tooltip("How many queued items are turned into fuel per completed cycle (mockup '5x').")]
        [SerializeField, Min(1)] private int batchSize = 1;

        [Header("SFX (optional)")]
        [SerializeField] private SfxSource acceptSfx;
        [SerializeField] private SfxSource produceSfx;

        // Items waiting to be converted.
        private int _queued;
        private float _progress; // 0..1 toward the next conversion tick

        public int Queued => _queued;
        public float Progress => Mathf.Clamp01(_progress);
        public ItemType Accepts => accepts;

        public event Action OnChanged;

        // Eggs converted per second (from stats). Mockup default: 1 per minute.
        private float RatePerSecond =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0f, ServiceLocator.StatService.GameValue(GameStat.FuelConvertRate))
                : 1f / 60f;

        private float FuelPerItem =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0f, ServiceLocator.StatService.GameValue(GameStat.FuelPerEgg))
                : 1f;

        // Called by ResourceHole when a matching item is dropped in.
        public void Accept(Item item)
        {
            if (item == null) return;

            bool matches = accepts != null && item.type != null &&
                           item.type.GetType() == accepts.GetType();

            if (!matches) return; // let other holes/systems handle non-matching items

            _queued++;
            if (acceptSfx != null) acceptSfx.Play();
            OnChanged?.Invoke();
            Destroy(item.gameObject);
        }

        private void Update()
        {
            if (_queued <= 0) { _progress = 0f; return; }
            if (ServiceLocator.FuelService == null) return;

            float rate = RatePerSecond;
            if (rate <= 0f) return;

            // Each full unit of progress converts one item.
            _progress += rate * Time.deltaTime;

            int converted = 0;
            while (_progress >= 1f && _queued > 0 && converted < batchSize)
            {
                _progress -= 1f;
                _queued--;
                converted++;
            }

            if (converted > 0)
            {
                ServiceLocator.FuelService.Add(FuelPerItem * converted);
                if (produceSfx != null) produceSfx.Play();
                OnChanged?.Invoke();
            }
        }
    }
}
