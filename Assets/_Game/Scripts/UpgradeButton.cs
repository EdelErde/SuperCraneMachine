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
        [Tooltip("Second price line shown below the money cost when the upgrade also costs fuel.")]
        [SerializeField] private TextMeshProUGUI fuelCostLabel;
        [Tooltip("Format for the fuel cost line. {0} = fuel amount.")]
        [SerializeField] private string fuelCostFormat = "Fuel {0}";
        [SerializeField] private Image icon;
        [SerializeField] private Color affordableColor = Color.white;
        [SerializeField] private Color unaffordableColor = new Color(0.5f, 0.5f, 0.5f);

        [Header("Preview (locked teaser)")]
        [SerializeField] private string previewName = "???";
        [Tooltip("{0} = gating upgrade name, {1} = required level.")]
        [SerializeField] private string previewCostFormat = "Unlocked by {0} Lv.{1}";
        [SerializeField] private Color previewColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color previewIconColor = new Color(0f, 0f, 0f, 0.5f);

        [Header("Level pips")]
        [Tooltip("Square pip image, spawned once per purchasable level. Leave empty to disable pips.")]
        [SerializeField] private Image pipPrefab;
        [Tooltip("Parent the pips spawn under (give it a horizontal layout group).")]
        [SerializeField] private RectTransform pipContainer;
        [Tooltip("Color for a level already bought.")]
        [SerializeField] private Color pipBoughtColor = new Color(0.80f, 0.42f, 0.09f);   // dark orange, filled
        [Tooltip("Color for a level still available to buy.")]
        [SerializeField] private Color pipOpenColor = new Color(0.80f, 0.42f, 0.09f, 0.28f); // dark orange, dim
        [Tooltip("Hide the pip row for single-purchase upgrades (unlocks, Auto Magnet, etc.).")]
        [SerializeField] private bool hidePipsForSinglePurchase = true;

        public IUpgrade Upgrade => upgrade;

        private readonly System.Collections.Generic.List<Image> _pips =
            new System.Collections.Generic.List<Image>();
        private int _pipsBuiltFor = -1;

        // Sets name, cost and icon from the upgrade. Safe to run in edit mode.
        public void ApplyStaticVisuals()
        {
            if (upgrade == null) return;

            if (label != null) label.text = upgrade.DisplayName;
            if (costLabel != null) costLabel.text = NumberFormat.Money(upgrade.CurrentCost);
            UpdateFuelCostLabel();

            if (icon != null && !string.IsNullOrEmpty(upgrade.IconPath))
            {
                var sprite = Resources.Load<Sprite>(upgrade.IconPath);
                if (sprite != null) icon.sprite = sprite;
            }
        }

        private Button _button;
        private bool _initialized;

        private void Awake() => _button = GetComponent<Button>();

        // Idempotent setup: registers listeners + the button with the service and does a
        // first refresh. Safe to call from a parent view before the object is active (which
        // is why this is not just Start) — and Start calls it as a fallback for hand-placed
        // buttons that no view initializes.
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (_button == null) _button = GetComponent<Button>();

            _button.onClick.AddListener(OnClick);
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyChanged += OnMoneyChanged;
            if (ServiceLocator.FuelService != null)
                ServiceLocator.FuelService.OnFuelChanged += OnFuelChanged;
            if (ServiceLocator.UpgradeService != null)
            {
                ServiceLocator.UpgradeService.RegisterButton(this);
                ServiceLocator.UpgradeService.OnUpgradesChanged += Refresh;
            }
            Refresh();
        }

        private void Start() => Initialize();

        private void OnDestroy()
        {
            if (!_initialized) return;

            _button.onClick.RemoveListener(OnClick);
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyChanged -= OnMoneyChanged;
            if (ServiceLocator.FuelService != null)
                ServiceLocator.FuelService.OnFuelChanged -= OnFuelChanged;
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
        private void OnFuelChanged(float _) => Refresh();

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
                SetPipsVisible(false);
                return;
            }

            if (label != null) label.text = upgrade.Label;
            if (costLabel != null)
                costLabel.text = upgrade.MaxedOut ? "MAX" : NumberFormat.Money(upgrade.CurrentCost);
            UpdateFuelCostLabel();
            if (icon != null) icon.color = Color.white;

            RefreshPips();

            bool affordable = ServiceLocator.UpgradeService.CanAfford(upgrade);
            _button.interactable = affordable;

            var colors = _button.colors;
            colors.normalColor = affordable ? affordableColor : unaffordableColor;
            _button.colors = colors;
        }

        // ---- Level pips ----
        private void RefreshPips()
        {
            if (pipPrefab == null || pipContainer == null) { return; }

            int max = upgrade != null ? upgrade.MaxPurchases : 0;

            // Single-purchase (or unbounded, max<=0): optionally hide the whole row.
            if (max <= 1 && hidePipsForSinglePurchase)
            {
                SetPipsVisible(false);
                return;
            }
            SetPipsVisible(true);

            if (_pipsBuiltFor != max) BuildPips(max);

            int bought = upgrade.TimesPurchased;
            for (int i = 0; i < _pips.Count; i++)
                _pips[i].color = i < bought ? pipBoughtColor : pipOpenColor;
        }

        private void BuildPips(int count)
        {
            // grow
            while (_pips.Count < count)
                _pips.Add(Instantiate(pipPrefab, pipContainer));
            // shrink (hide extras)
            for (int i = 0; i < _pips.Count; i++)
                _pips[i].gameObject.SetActive(i < count);
            _pipsBuiltFor = count;
        }

        private void SetPipsVisible(bool visible)
        {
            if (pipContainer != null && pipContainer.gameObject.activeSelf != visible)
                pipContainer.gameObject.SetActive(visible);
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

            if (fuelCostLabel != null) fuelCostLabel.gameObject.SetActive(false);

            if (icon != null) icon.color = previewIconColor;

            var colors = _button.colors;
            colors.normalColor = previewColor;
            colors.disabledColor = previewColor;
            _button.colors = colors;
        }

        // Shows/hides the second price line. Only visible when the upgrade actually costs
        // fuel and isn't maxed. Tinted red when the player can't currently afford the fuel.
        private void UpdateFuelCostLabel()
        {
            if (fuelCostLabel == null) return;

            int fuelCost = upgrade != null ? upgrade.CurrentFuelCost : 0;
            bool show = fuelCost > 0 && upgrade != null && !upgrade.MaxedOut;

            fuelCostLabel.gameObject.SetActive(show);
            if (!show) return;

            fuelCostLabel.text = string.Format(fuelCostFormat, NumberFormat.Abbreviate(fuelCost));

            var fuel = ServiceLocator.FuelService;
            bool haveFuel = fuel != null && fuel.Has(fuelCost);
            fuelCostLabel.color = haveFuel ? affordableColor : unaffordableColor;
        }
    }
}