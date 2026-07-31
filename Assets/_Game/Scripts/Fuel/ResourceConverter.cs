using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class ResourceConverter : MonoBehaviour
    {
        [Header("Display")]
        [Tooltip("Recipe title shown on the production card, e.g. 'Egg \u2192 Fuel'.")]
        [SerializeField] private string recipeTitle = "Egg \u2192 Fuel";
        [Tooltip("Name of the resource this produces, e.g. 'Fuel'. For labels/tooltips.")]
        [SerializeField] private string outputName = "Fuel";

        [Tooltip("Which item type this converter accepts. Others are ignored (left for other holes).")]
        [SerializeReference] private ItemType accepts = new Egg();

        [Tooltip("How many queued items are turned into fuel per completed cycle (mockup '5x').")]
        [SerializeField, Min(1)] private int batchSize = 1;

        [Header("SFX (optional)")]
        [SerializeField] private SfxSource acceptSfx;
        [SerializeField] private SfxSource produceSfx;

        [Header("Debug")]
        [Tooltip("Log accepts/conversions to the Console to diagnose the pipeline.")]
        [SerializeField] private bool debugLog = false;

        private int _queued;
        private float _progress;

        public int Queued => _queued;
        public float Progress => Mathf.Clamp01(_progress);
        public ItemType Accepts => accepts;
        public int BatchSize => Mathf.Max(1, batchSize);
        public string RecipeTitle => recipeTitle;
        public string OutputName => outputName;

        public float RatePerMinute => RatePerSecond * 60f;

        private void OnEnable()
        {
            if (ServiceLocator.ResourceConverters != null)
                ServiceLocator.ResourceConverters.Register(this);
        }

        private void OnDisable()
        {
            if (ServiceLocator.ResourceConverters != null)
                ServiceLocator.ResourceConverters.Unregister(this);
        }

        public event Action OnChanged;

        private float RatePerSecond =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0f, ServiceLocator.StatService.GameValue(GameStat.FuelConvertRate))
                : 1f / 60f;

        private float FuelPerItem =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0f, ServiceLocator.StatService.GameValue(GameStat.FuelPerEgg))
                : 1f;

        public void Accept(Item item)
        {
            if (item == null) return;

            bool matches = accepts != null && item.type != null &&
                           item.type.GetType() == accepts.GetType();

            if (!matches) return;

            _queued++;
            if (debugLog) Debug.Log($"[Converter] accepted {item.type.GetType().Name}; queued={_queued}", this);
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
                if (debugLog) Debug.Log($"[Converter] converted {converted}; queued={_queued}; +{FuelPerItem * converted} fuel", this);
                if (produceSfx != null) produceSfx.Play();
                OnChanged?.Invoke();
            }
        }
    }
}