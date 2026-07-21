using TMPro;
using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class MoneyDisplay : MonoBehaviour
    {
        [SerializeField] private string format = "${0}";

        private TextMeshProUGUI _label;

        private void Awake() => _label = GetComponent<TextMeshProUGUI>();

        private void Start()
        {
            var stats = ServiceLocator.StatService;
            if (stats == null) return;

            stats.OnMoneyChanged += Refresh;
            Refresh(stats.CurrentMoney);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyChanged -= Refresh;
        }

        private void Refresh(int money) => _label.text = string.Format(format, money);
    }
}