using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    [RequireComponent(typeof(Button))]
    public class UpgradeButton : MonoBehaviour
    {
        [Header("Upgrade")]
        [SerializeReference, UpgradeReference] private IUpgrade upgrade;

        [Header("Unlock gate (optional)")]
        [Tooltip("Button stays hidden until the gating upgrade reaches the required level. Leave as None to be visible from the start.")]
        [SerializeReference, UpgradeReference] private IUpgrade unlockedBy;
        [Tooltip("How many times the gating upgrade must be bought before this unlocks.")]
        [SerializeField, Min(1)] private int requiredLevel = 1;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI costLabel;
        [SerializeField] private Image icon;
        [SerializeField] private Color affordableColor = Color.white;
        [SerializeField] private Color unaffordableColor = new Color(0.5f, 0.5f, 0.5f);

        public IUpgrade Upgrade => upgrade;

        public void ApplyStaticVisuals()
        {
            if (upgrade == null) return;

            if (label != null) label.text = upgrade.DisplayName;
            if (costLabel != null) costLabel.text = $"${upgrade.CurrentCost}";

            if (icon != null && !string.IsNullOrEmpty(upgrade.IconPath))
            {
                var sprite = Resources.Load<Sprite>(upgrade.IconPath);
                if (sprite != null) icon.sprite = sprite;
            }
        }

        private Button _button;

        private void Awake() => _button = GetComponent<Button>();

        private void Start()
        {
            _button.onClick.AddListener(OnClick);
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyChanged += OnMoneyChanged;
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyChanged -= OnMoneyChanged;
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged -= Refresh;
        }

        private void OnClick()
        {
            ServiceLocator.UpgradeService.TryBuy(upgrade);
            Refresh();
        }

        private void OnMoneyChanged(int _) => Refresh();

        public bool Unlocked =>
            unlockedBy == null ||
            (ServiceLocator.UpgradeService != null &&
             ServiceLocator.UpgradeService.TimesPurchased(unlockedBy.GetType()) >= requiredLevel);

        private void Refresh()
        {
            bool unlocked = Unlocked;
            if (gameObject.activeSelf != unlocked)
                gameObject.SetActive(unlocked);
            if (!unlocked) return;

            if (label != null) label.text = upgrade.Label;
            if (costLabel != null)
                costLabel.text = upgrade.MaxedOut ? "MAX" : $"${upgrade.CurrentCost}";

            bool affordable = ServiceLocator.UpgradeService.CanAfford(upgrade);
            _button.interactable = affordable;

            var colors = _button.colors;
            colors.normalColor = affordable ? affordableColor : unaffordableColor;
            _button.colors = colors;
        }
    }
}