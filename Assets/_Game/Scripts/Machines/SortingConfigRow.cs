using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // One row in the sorting config window: an item label, a 0..1 slider for the share
    // that goes to hole B, and a percentage readout. The rest goes to hole A.
    public class SortingConfigRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private Slider ratioSlider;   // 0..1
        [SerializeField] private TextMeshProUGUI valueLabel;
        [SerializeField] private Image icon;

        private Action<float> _onChanged;

        public void Bind(ItemType type, float ratioToB, Action<float> onChanged, Sprite sprite = null)
        {
            _onChanged = onChanged;

            if (nameLabel != null) nameLabel.text = type != null ? type.DisplayName : "?";

            if (icon != null)
            {
                icon.enabled = sprite != null;
                if (sprite != null) icon.sprite = sprite;
            }

            if (ratioSlider != null)
            {
                ratioSlider.minValue = 0f;
                ratioSlider.maxValue = 1f;
                ratioSlider.SetValueWithoutNotify(Mathf.Clamp01(ratioToB));
                ratioSlider.onValueChanged.RemoveListener(HandleSlider);
                ratioSlider.onValueChanged.AddListener(HandleSlider);
            }

            UpdateValueLabel(ratioToB);
        }

        private void HandleSlider(float value)
        {
            UpdateValueLabel(value);
            _onChanged?.Invoke(value);
        }

        private void UpdateValueLabel(float ratioToB)
        {
            if (valueLabel == null) return;
            int pctB = Mathf.RoundToInt(Mathf.Clamp01(ratioToB) * 100f);
            valueLabel.text = $"B {pctB}%  /  A {100 - pctB}%";
        }

        private void OnDestroy()
        {
            if (ratioSlider != null)
                ratioSlider.onValueChanged.RemoveListener(HandleSlider);
        }
    }
}