using TMPro;
using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class MoneyDisplay : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private string format = "${0}";
        [Tooltip("How fast the shown number counts toward the real value.")]
        [SerializeField] private float countSpeed = 6f;
        [Tooltip("Snap instantly if the gap is larger than this.")]
        [SerializeField] private int snapThreshold = 100000;

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

        private int _target;
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
            var stats = ServiceLocator.StatService;
            if (stats == null) return;

            stats.OnMoneyChanged += OnChanged;
            _target = stats.CurrentMoney;
            _shown = _target;
            Render();
        }

        private void OnDestroy()
        {
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyChanged -= OnChanged;
        }

        private void OnChanged(int money)
        {
            _spending = money < _target;
            _target = money;

            if (pop) _pop = 1f;
            if (Mathf.Abs(_target - _shown) > snapThreshold) _shown = _target;
        }

        private void Update()
        {
            if (!Mathf.Approximately(_shown, _target))
            {
                _shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-countSpeed * Time.deltaTime));
                if (Mathf.Abs(_target - _shown) < 0.5f) _shown = _target;
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