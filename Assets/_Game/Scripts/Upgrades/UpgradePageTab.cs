using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // A tab in the upgrade window's top row. Shows the page number/title, a lock icon +
    // requirement text while the page is locked, and selects the page when clicked.
    [RequireComponent(typeof(Button))]
    public class UpgradePageTab : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;      // page number or title
        [SerializeField] private TextMeshProUGUI requirementLabel; // shown while locked
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private Image fill;                 // optional progress fill
        [SerializeField] private GameObject selectedMarker;  // optional "active" highlight

        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f);
        [SerializeField] private Color selectedColor = new Color(0.80f, 0.42f, 0.09f);

        private Button _button;
        private UpgradePage _page;
        private int _index;
        private System.Action<int> _onSelect;
        private string _pageNumber;

        private void Awake() => _button = GetComponent<Button>();

        public void Bind(int index, UpgradePage page, System.Action<int> onSelect)
        {
            _index = index;
            _page = page;
            _onSelect = onSelect;

            _pageNumber = (index + 1).ToString();

            _button.onClick.RemoveListener(Click);
            _button.onClick.AddListener(Click);

            Refresh(false, false);
        }

        private void Click()
        {
            if (_page != null && !_page.IsUnlocked) return;
            _onSelect?.Invoke(_index);
        }

        public void Refresh(bool selected, bool isNextToUnlock)
        {
            bool unlocked = _page == null || _page.IsUnlocked;
            bool showRequirement = !unlocked && isNextToUnlock;

            if (lockIcon != null) lockIcon.SetActive(!unlocked);

            if (requirementLabel != null)
            {
                if (showRequirement)
                    requirementLabel.text = _page != null ? _page.Requirement : "";
                else if (!unlocked)
                    requirementLabel.text = "???";
                else
                    requirementLabel.text = "";
            }

            if (label != null)
                label.text = unlocked ? _pageNumber : "";

            if (fill != null)
                fill.fillAmount = _page != null ? _page.Unlock.Progress : 1f;

            if (selectedMarker != null) selectedMarker.SetActive(selected && unlocked);

            _button.interactable = unlocked;

            var colors = _button.colors;
            colors.normalColor = !unlocked ? lockedColor : (selected ? selectedColor : unlockedColor);
            _button.colors = colors;
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(Click);
        }
    }
}