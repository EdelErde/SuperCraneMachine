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

        [Header("Preview (locked teaser)")]
        [SerializeField] private string previewName = "???";
        [Tooltip("{0} = gating upgrade name, {1} = required level.")]
        [SerializeField] private string previewCostFormat = "Unlocked by {0} Lv.{1}";
        [SerializeField] private Color previewColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color previewIconColor = new Color(0f, 0f, 0f, 0.5f);

        public IUpgrade Upgrade => upgrade;

        // Sets name, cost and icon from the upgrade. Safe to run in edit mode.
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
            {
                ServiceLocator.UpgradeService.RegisterButton(this);
                ServiceLocator.UpgradeService.OnUpgradesChanged += Refresh;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyChanged -= OnMoneyChanged;
            if (ServiceLocator.UpgradeService != null)
            {
                ServiceLocator.UpgradeService.UnregisterButton(this);
                ServiceLocator.UpgradeService.OnUpgradesChanged -= Refresh;
            }
        }

        private void OnClick()
        {
            if (!Unlocked) return;
            ServiceLocator.UpgradeService.TryBuy(upgrade);
            Refresh();
        }

        private void OnMoneyChanged(int _) => Refresh();

        public bool Unlocked =>
            unlockedBy == null ||
            (ServiceLocator.UpgradeService != null &&
             ServiceLocator.UpgradeService.TimesPurchased(unlockedBy.GetType()) >= requiredLevel);

        // Shown as a locked teaser: the gating upgrade is itself visible,
        // but hasn't reached the required level yet.
        public bool IsPreview
        {
            get
            {
                if (unlockedBy == null || Unlocked) return false;
                var svc = ServiceLocator.UpgradeService;
                if (svc == null) return false;

                var gateButton = svc.FindButton(unlockedBy.GetType());
                // No button for the gate -> show the teaser anyway.
                return gateButton == null || gateButton.Unlocked;
            }
        }

        public bool Visible => Unlocked || IsPreview;

        public void ForceRefresh() => Refresh();

        private void Refresh()
        {
            bool visible = Visible;
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
            if (!visible) return;

            if (IsPreview)
            {
                ShowPreview();
                return;
            }

            if (label != null) label.text = upgrade.Label;
            if (costLabel != null)
                costLabel.text = upgrade.MaxedOut ? "MAX" : $"${upgrade.CurrentCost}";
            if (icon != null) icon.color = Color.white;

            bool affordable = ServiceLocator.UpgradeService.CanAfford(upgrade);
            _button.interactable = affordable;

            var colors = _button.colors;
            colors.normalColor = affordable ? affordableColor : unaffordableColor;
            _button.colors = colors;
        }

        private void ShowPreview()
        {
            _button.interactable = false;

            if (label != null) label.text = previewName;

            if (costLabel != null)
            {
                string gateName = unlockedBy != null ? unlockedBy.DisplayName : "?";
                costLabel.text = string.Format(previewCostFormat, gateName, requiredLevel);
            }

            if (icon != null) icon.color = previewIconColor;

            var colors = _button.colors;
            colors.normalColor = previewColor;
            colors.disabledColor = previewColor;
            _button.colors = colors;
        }
    }
}