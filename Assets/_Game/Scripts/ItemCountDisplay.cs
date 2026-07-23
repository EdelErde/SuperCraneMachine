using TMPro;
using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ItemCountDisplay : MonoBehaviour
    {
        [SerializeField] private string format = "{0} / {1}";
        [SerializeField] private float refreshInterval = 0.2f;

        private TextMeshProUGUI _label;
        private float _timer;

        private void Awake() => _label = GetComponent<TextMeshProUGUI>();

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = refreshInterval;

            var spawner = ServiceLocator.ItemSpawner;
            if (spawner == null) return;

            _label.text = string.Format(format, spawner.LiveCount, spawner.MaxCount);
        }
    }
}