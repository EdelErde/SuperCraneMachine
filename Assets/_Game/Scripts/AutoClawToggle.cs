using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    [RequireComponent(typeof(Button))]
    public class AutoClawToggle : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image indicator;
        [SerializeField] private string onText = "Auto: ON";
        [SerializeField] private string offText = "Auto: OFF";
        [SerializeField] private Color onColor = new Color(0.4f, 1f, 0.45f);
        [SerializeField] private Color offColor = new Color(0.55f, 0.55f, 0.55f);
        [Tooltip("Hide the button until the Auto Claw upgrade is bought.")]
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
            var claw = ServiceLocator.Claw;
            if (claw == null) return;

            claw.ToggleAutoClaw();
            Refresh();
        }

        private void Refresh()
        {
            var claw = ServiceLocator.Claw;
            if (claw == null) return;

            if (hideUntilUnlocked)
            {
                bool unlocked = claw.AutoClawUnlocked;
                if (gameObject.activeSelf != unlocked)
                    gameObject.SetActive(unlocked);
                if (!unlocked) return;
            }

            bool on = claw.AutoClawEnabled;
            if (label != null) label.text = on ? onText : offText;
            if (indicator != null) indicator.color = on ? onColor : offColor;
        }
    }
}