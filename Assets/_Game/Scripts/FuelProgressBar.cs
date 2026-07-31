using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // Drives a small progress fill (e.g. behind the top-bar Fuel readout) from the egg->fuel
    // converter's current progress toward the next egg. When nothing is queued, the bar reads
    // empty (or holds, configurable).
    public class FuelProgressBar : MonoBehaviour
    {
        [Tooltip("Converter to read progress from. Auto-found if left empty.")]
        [SerializeField] private ResourceConverter converter;

        [Tooltip("Filled image (Image Type = Filled). Its fillAmount is driven 0..1.")]
        [SerializeField] private Image fill;

        [Tooltip("Smooth the fill instead of snapping (0 = instant).")]
        [SerializeField] private float smoothing = 10f;

        [Tooltip("When nothing is queued, drop the bar to empty. If false, it holds its value.")]
        [SerializeField] private bool emptyWhenIdle = true;

        [Tooltip("Optional: hide the fill entirely when idle.")]
        [SerializeField] private bool hideWhenIdle = false;

        private float _shown;

        private void OnEnable()
        {
            if (converter == null) converter = FindConverter();
        }

        private void Update()
        {
            if (fill == null) return;

            bool active = converter != null && converter.Queued > 0;
            float target = active ? converter.Progress : (emptyWhenIdle ? 0f : _shown);

            _shown = smoothing > 0f
                ? Mathf.Lerp(_shown, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime))
                : target;

            fill.fillAmount = _shown;

            if (hideWhenIdle && fill.enabled != active)
                fill.enabled = active;
        }

        private static ResourceConverter FindConverter()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<ResourceConverter>();
#else
            return Object.FindObjectOfType<ResourceConverter>();
#endif
        }
    }
}