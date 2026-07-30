using TMPro;
using UnityEngine;

namespace CraneMachine
{
    // HUD readout for the shared fuel pool. Mirrors MoneyDisplay's smooth-count + pop feel.
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FuelDisplay : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private string format = "Fuel {0}";
        [Tooltip("How fast the shown number counts toward the real value.")]
        [SerializeField] private float countSpeed = 6f;
        [Tooltip("Snap instantly if the gap is larger than this.")]
        [SerializeField] private float snapThreshold = 10000f;

        [Header("Pop")]
        [SerializeField] private bool pop = true;
        [SerializeField] private float gainScale = 1.15f;
        [SerializeField] private float spendScale = 0.9f;
        [SerializeField] private float popDecay = 5f;
        [SerializeField] private Color gainColor = new Color(0.45f, 1f, 0.5f);
        [SerializeField] private Color spendColor = new Color(1f, 0.5f, 0.45f);

        private TextMeshProUGUI _label;
        private RectTransform _rect;
        private Color _baseColor;
        private Vector3 _baseScale;

        private float _target;
        private float _shown;
        private float _pop;
        private bool _spending;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            _rect = GetComponent<RectTransform>();
            _baseColor = _label.color;
            _baseScale = _rect.localScale;
        }

        private void Start()
        {
            var fuel = ServiceLocator.FuelService;
            if (fuel == null) return;

            fuel.OnFuelChanged += OnChanged;
            _target = fuel.CurrentFuel;
            _shown = _target;
            Render();
        }

        private void OnDestroy()
        {
            if (ServiceLocator.FuelService != null)
                ServiceLocator.FuelService.OnFuelChanged -= OnChanged;
        }

        private void OnChanged(float fuel)
        {
            _spending = fuel < _target;
            _target = fuel;

            if (pop) _pop = 1f;
            if (Mathf.Abs(_target - _shown) > snapThreshold) _shown = _target;
        }

        private void Update()
        {
            if (!Mathf.Approximately(_shown, _target))
            {
                _shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-countSpeed * Time.deltaTime));
                if (Mathf.Abs(_target - _shown) < 0.05f) _shown = _target;
            }

            Render();

            if (pop) UpdatePop();
        }

        private void Render()
            => _label.text = string.Format(format, Mathf.RoundToInt(_shown));

        private void UpdatePop()
        {
            if (_pop > 0f)
                _pop = Mathf.Max(0f, _pop - popDecay * Time.deltaTime);

            float t = _pop * _pop;
            float scale = _spending ? spendScale : gainScale;
            Color c = _spending ? spendColor : gainColor;

            _rect.localScale = _baseScale * Mathf.Lerp(1f, scale, t);
            _label.color = Color.Lerp(_baseColor, c, t);
        }
    }
}
