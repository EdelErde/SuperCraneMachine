using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    [RequireComponent(typeof(Button))]
    public class AutoMagnetToggle : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image indicator;
        [SerializeField] private string onText = "Auto: ON";
        [SerializeField] private string offText = "Auto: OFF";
        [SerializeField] private Color onColor = new Color(0.4f, 1f, 0.45f);
        [SerializeField] private Color offColor = new Color(0.55f, 0.55f, 0.55f);
        [Tooltip("Hide the button until the Auto Magnet upgrade is bought.")]
        [SerializeField] private bool hideUntilUnlocked = true;

        private Button _button;

        private void Awake() => _button = GetComponent<Button>();

        private void Start()
        {
            _button.onClick.AddListener(OnClick);
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged -= Refresh;
        }

        private void OnClick()
        {
            var magnet = ServiceLocator.Magnet;
            if (magnet == null) return;

            magnet.ToggleAutoMagnet();
            Refresh();
        }

        private void Refresh()
        {
            var magnet = ServiceLocator.Magnet;
            if (magnet == null) return;

            if (hideUntilUnlocked)
            {
                bool unlocked = magnet.AutoMagnetUnlocked;
                if (gameObject.activeSelf != unlocked)
                    gameObject.SetActive(unlocked);
                if (!unlocked) return;
            }

            bool on = magnet.AutoMagnetEnabled;
            if (label != null) label.text = on ? onText : offText;
            if (indicator != null) indicator.color = on ? onColor : offColor;
        }
    }
}