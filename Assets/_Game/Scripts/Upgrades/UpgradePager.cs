using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Runtime controller for the paged upgrade window. Pairs each UpgradePage with a tab in
    // the top row, shows one page at a time, and refreshes lock state whenever upgrades change
    // (so a page unlocks live the moment its requirement is met).
    //
    // Pages and tabs are created by the editor build step (UpgradeViewEditor) or wired by hand;
    // this component just discovers and drives them.
    public class UpgradePager : MonoBehaviour
    {
        [Tooltip("Pages in tab order. If empty, found under 'pageParent' at Start.")]
        [SerializeField] private List<UpgradePage> pages = new List<UpgradePage>();
        [Tooltip("Tabs in the same order as pages. If empty, found under 'tabParent' at Start.")]
        [SerializeField] private List<UpgradePageTab> tabs = new List<UpgradePageTab>();

        [Tooltip("Optional parent to auto-discover pages from (children order = tab order).")]
        [SerializeField] private Transform pageParent;
        [Tooltip("Optional parent to auto-discover tabs from (children order = tab order).")]
        [SerializeField] private Transform tabParent;

        private int _current = -1;

        private void Start()
        {
            AutoDiscover();
            BindTabs();

            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged += OnUpgradesChanged;

            SelectFirstUnlocked();
            RefreshTabs();
        }

        private void OnDestroy()
        {
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged -= OnUpgradesChanged;
        }

        private void AutoDiscover()
        {
            if (pages.Count == 0 && pageParent != null)
                pageParent.GetComponentsInChildren(true, pages);
            if (tabs.Count == 0 && tabParent != null)
                tabParent.GetComponentsInChildren(true, tabs);
        }

        private void BindTabs()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i] == null) continue;
                var page = i < pages.Count ? pages[i] : null;
                tabs[i].Bind(i, page, Select);
            }
        }

        private void OnUpgradesChanged()
        {
            // A locked current page might have just unlocked, or vice versa.
            RefreshTabs();
            if (_current >= 0 && _current < pages.Count && pages[_current] != null)
                pages[_current].RefreshLockState();
        }

        public void Select(int index)
        {
            if (index < 0 || index >= pages.Count) return;
            if (pages[index] != null && !pages[index].IsUnlocked) return;

            for (int i = 0; i < pages.Count; i++)
                if (pages[i] != null) pages[i].SetSelected(i == index);

            _current = index;
            RefreshTabs();
        }

        private void SelectFirstUnlocked()
        {
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] != null && pages[i].IsUnlocked) { Select(i); return; }
            }
            // Fallback: show the first page even if somehow all locked.
            if (pages.Count > 0) Select(0);
        }

        private void RefreshTabs()
        {
            int nextToUnlock = NextPageToUnlock();
            for (int i = 0; i < tabs.Count; i++)
                if (tabs[i] != null) tabs[i].Refresh(i == _current, i == nextToUnlock);
        }

        // Index of the first locked page in tab order (the only one that should show its
        // requirement). Returns -1 when every page is unlocked.
        private int NextPageToUnlock()
        {
            for (int i = 0; i < pages.Count; i++)
                if (pages[i] != null && !pages[i].IsUnlocked)
                    return i;
            return -1;
        }
    }
}