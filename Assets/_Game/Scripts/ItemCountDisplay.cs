using TMPro;
using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ItemCountDisplay : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private string format = "{0} / {1}";
        [SerializeField] private float refreshInterval = 0.15f;

        [Header("Fill colour")]
        [Tooltip("Tint the text as the tank fills up.")]
        [SerializeField] private bool tintOnFill = true;
        [SerializeField] private Color emptyColor = Color.white;
        [SerializeField] private Color fullColor = new Color(1f, 0.55f, 0.4f);
        [Tooltip("Fill fraction where the colour starts shifting.")]
        [Range(0f, 1f)]
        [SerializeField] private float tintStart = 0.6f;

        [Header("Pop")]
        [SerializeField] private bool pop = true;
        [SerializeField] private float addScale = 1.12f;
        [SerializeField] private float removeScale = 0.92f;
        [SerializeField] private float popDecay = 6f;

        [Header("Full pulse")]
        [Tooltip("Gently pulse while the tank is at capacity.")]
        [SerializeField] private bool pulseWhenFull = true;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseAmount = 0.06f;

        private TextMeshProUGUI _label;
        private RectTransform _rect;
        private Vector3 _baseScale;

        private float _timer;
        private int _last = -1;
        private float _pop;
        private bool _removing;
        private bool _full;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            _rect = GetComponent<RectTransform>();
            _baseScale = _rect.localScale;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = refreshInterval;
                Sample();
            }

            UpdateVisuals();
        }

        private void Sample()
        {
            var spawner = ServiceLocator.ItemSpawner;
            if (spawner == null) return;

            int count = spawner.LiveCount;
            int max = spawner.MaxCount;

            if (count != _last)
            {
                if (_last >= 0 && pop)
                {
                    _removing = count < _last;
                    _pop = 1f;
                }
                _last = count;
            }

            _full = max > 0 && count >= max;
            _label.text = string.Format(format, count, max);

            if (tintOnFill && max > 0)
            {
                float fill = Mathf.Clamp01((float)count / max);
                float t = Mathf.InverseLerp(tintStart, 1f, fill);
                _label.color = Color.Lerp(emptyColor, fullColor, t);
            }
        }

        private void UpdateVisuals()
        {
            float scale = 1f;

            if (pop)
            {
                if (_pop > 0f)
                    _pop = Mathf.Max(0f, _pop - popDecay * Time.deltaTime);

                float t = _pop * _pop;
                scale *= Mathf.Lerp(1f, _removing ? removeScale : addScale, t);
            }

            if (pulseWhenFull && _full)
                scale *= 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

            _rect.localScale = _baseScale * scale;
        }
    }
}