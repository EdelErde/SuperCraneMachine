using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // One resource-production card (the "Egg -> Fuel / 1/min / 5x" panel from the mockup),
    // bound to a single ResourceConverter. The ProductionView spawns one of these per
    // converter at runtime, so new resources need no new UI wiring.
    public class ProductionCard : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI titleLabel;   // "Egg -> Fuel"
        [SerializeField] private Image inputIcon;              // egg icon
        [SerializeField] private TextMeshProUGUI batchLabel;   // "5x"
        [SerializeField] private TextMeshProUGUI rateLabel;    // "1/min"
        [SerializeField] private string rateFormat = "{0:0.#}/min";
        [SerializeField] private Image progressFill;           // Image Type = Filled
        [SerializeField] private TextMeshProUGUI queuedLabel;  // optional "{0} queued"
        [SerializeField] private string queuedFormat = "{0} queued";

        private ResourceConverter _converter;

        public ResourceConverter Converter => _converter;

        // Called by ProductionView right after Instantiate.
        public void Bind(ResourceConverter converter)
        {
            if (_converter != null) _converter.OnChanged -= RefreshData;
            _converter = converter;
            if (_converter != null) _converter.OnChanged += RefreshData;

            RefreshStatic();
            RefreshData();
        }

        private void OnDestroy()
        {
            if (_converter != null) _converter.OnChanged -= RefreshData;
        }

        private void Update()
        {
            // Progress advances continuously while converting; poll the fill each frame.
            if (progressFill != null && _converter != null)
                progressFill.fillAmount = _converter.Progress;
        }

        private void RefreshStatic()
        {
            if (_converter == null) return;

            if (titleLabel != null) titleLabel.text = _converter.RecipeTitle;

            if (inputIcon != null)
            {
                var sprite = IconFor(_converter.Accepts);
                inputIcon.enabled = sprite != null;
                if (sprite != null) inputIcon.sprite = sprite;
            }
        }

        private void RefreshData()
        {
            if (_converter == null) return;

            if (batchLabel != null) batchLabel.text = $"{_converter.BatchSize}x";
            if (rateLabel != null) rateLabel.text = string.Format(rateFormat, _converter.RatePerMinute);
            if (queuedLabel != null)
                queuedLabel.text = string.Format(queuedFormat, NumberFormat.Abbreviate(_converter.Queued));
        }

        // Resolve the input item's icon from the item database (same source the rest of the UI uses).
        private static Sprite IconFor(ItemType type)
        {
            if (type == null || ServiceLocator.ItemSpawner == null) return null;
            var db = ServiceLocator.ItemSpawner.Database;
            if (db == null) return null;

            foreach (var prefab in db.Prefabs)
            {
                if (prefab == null) continue;
                var item = prefab.GetComponent<Item>();
                if (item == null || item.type == null) continue;
                if (item.type.GetType() != type.GetType()) continue;

                var img = prefab.GetComponentInChildren<Image>(true);
                if (img != null && img.sprite != null) return img.sprite;
                var sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null) return sr.sprite;
            }
            return null;
        }
    }
}