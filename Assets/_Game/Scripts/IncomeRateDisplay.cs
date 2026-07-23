using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class IncomeRateDisplay : MonoBehaviour
    {
        [SerializeField] private string format = "{0:0.0} $/s";
        [SerializeField] private float windowSeconds = 10f;
        [SerializeField] private float refreshInterval = 0.1f;

        private readonly Queue<(float time, int amount)> _earnings = new Queue<(float, int)>();
        private TextMeshProUGUI _label;
        private float _timer;
        private int _windowTotal;

        private void Awake() => _label = GetComponent<TextMeshProUGUI>();

        private void Start()
        {
            _startTime = Time.time;
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyEarned += Record;
            Refresh();
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
        }

        private void Update()
        {
            Trim();

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = refreshInterval;

            Refresh();
        }

        private void Trim()
        {
            float cutoff = Time.time - windowSeconds;
            while (_earnings.Count > 0 && _earnings.Peek().time < cutoff)
                _windowTotal -= _earnings.Dequeue().amount;
        }

        private float _startTime;

        private void Refresh()
        {
            float elapsed = Time.time - _startTime;
            float span = Mathf.Min(windowSeconds, Mathf.Max(0.01f, elapsed));
            float rate = _windowTotal / span;
            _label.text = string.Format(format, rate);
        }
    }
}