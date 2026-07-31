using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class IncomeRateDisplay : MonoBehaviour
    {
        [Header("Rate")]
        [Tooltip("Shorter = more reactive. 3-4s reads well.")]
        [SerializeField] private float windowSeconds = 4f;
        [Tooltip("How fast the shown number chases the real rate. Higher = snappier.")]
        [SerializeField] private float smoothing = 8f;

        [Header("Display")]
        [SerializeField] private string format = "{0} $/s";
        [Tooltip("Below this value show one decimal, above it show whole numbers.")]
        [SerializeField] private float decimalThreshold = 10f;

        [Header("Sale pop")]
        [SerializeField] private bool popOnSale = true;
        [SerializeField] private float popScale = 1.18f;
        [SerializeField] private float popDecay = 6f;
        [SerializeField] private Color popColor = new Color(0.45f, 1f, 0.5f);

        private readonly Queue<(float time, int amount)> _earnings = new Queue<(float, int)>();
        private TextMeshProUGUI _label;
        private RectTransform _rect;
        private Color _baseColor;
        private Vector3 _baseScale;

        private int _windowTotal;
        private float _shown;
        private float _pop;
        private float _startTime;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            _rect = GetComponent<RectTransform>();
            _baseColor = _label.color;
            _baseScale = _rect.localScale;
        }

        private void Start()
        {
            _startTime = Time.time;
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyEarned += Record;
        }

        private void OnDestroy()
        {
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyEarned -= Record;
        }

        private void Record(int amount)
        {
            _earnings.Enqueue((Time.time, amount));
            _windowTotal += amount;
            if (popOnSale) _pop = 1f;
        }

        private void Update()
        {
            Trim();

            float target = CurrentRate();
            _shown = Mathf.Lerp(_shown, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            if (_shown < 0.05f && target <= 0f) _shown = 0f;

            _label.text = string.Format(format, Formatted(_shown));

            if (popOnSale) UpdatePop();
        }

        private float CurrentRate()
        {
            float elapsed = Time.time - _startTime;
            float span = Mathf.Min(windowSeconds, Mathf.Max(0.25f, elapsed));
            return _windowTotal / span;
        }

        private void Trim()
        {
            float cutoff = Time.time - windowSeconds;
            while (_earnings.Count > 0 && _earnings.Peek().time < cutoff)
                _windowTotal -= _earnings.Dequeue().amount;
        }

        private string Formatted(float value)
        {
            return value < decimalThreshold
                ? value.ToString("0.0")
                : NumberFormat.Abbreviate(value);
        }

        private void UpdatePop()
        {
            if (_pop > 0f)
                _pop = Mathf.Max(0f, _pop - popDecay * Time.deltaTime);

            float t = _pop * _pop;   // ease out
            _rect.localScale = _baseScale * Mathf.Lerp(1f, popScale, t);
            _label.color = Color.Lerp(_baseColor, popColor, t);
        }
    }
}