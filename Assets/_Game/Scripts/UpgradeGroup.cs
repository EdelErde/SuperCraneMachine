using TMPro;
using UnityEngine;

namespace CraneMachine
{
    public class UpgradeGroup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private RectTransform buttonParent;
        [SerializeField] private GameObject visualRoot;

        public RectTransform ButtonParent => buttonParent;

        public void SetTitle(string text)
        {
            if (title != null) title.text = text;
        }

        private void Start()
        {
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged -= Refresh;
        }

        private void Refresh()
        {
            var root = visualRoot != null ? visualRoot : gameObject;
            root.SetActive(HasVisibleButton());
        }

        private bool HasVisibleButton()
        {
            if (buttonParent == null) return true;

            var buttons = buttonParent.GetComponentsInChildren<UpgradeButton>(true);
            foreach (var b in buttons)
                if (b.Visible) return true;

            return false;
        }
    }
}