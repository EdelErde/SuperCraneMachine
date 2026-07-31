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

        private bool _initialized;

        public void SetTitle(string text)
        {
            if (title != null) title.text = text;
        }

        // Idempotent setup: subscribe + first refresh. Callable by the parent view before this
        // object is active (Start doesn't run on inactive objects, which is why the view drives
        // this explicitly). Start calls it as a fallback for standalone use.
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged += Refresh;
            Refresh();
        }

        private void Start() => Initialize();

        private void OnDestroy()
        {
            if (!_initialized) return;
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