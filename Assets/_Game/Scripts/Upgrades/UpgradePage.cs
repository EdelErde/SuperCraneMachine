using TMPro;
using UnityEngine;

namespace CraneMachine
{
    // A single upgrade page. Holds a content area where UpgradeGroups are built, plus a
    // page-unlock condition. When locked, the page shows a requirement message instead.
    public class UpgradePage : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("Where UpgradeGroups are created for this page.")]
        [SerializeField] private RectTransform groupParent;
        [Tooltip("Shown when the page is unlocked.")]
        [SerializeField] private GameObject unlockedRoot;
        [Tooltip("Shown when the page is still locked (requirement teaser).")]
        [SerializeField] private GameObject lockedRoot;
        [Tooltip("Label on the locked overlay describing what unlocks the page.")]
        [SerializeField] private TextMeshProUGUI lockedLabel;

        [Header("Unlock")]
        [SerializeField] private PageUnlockCondition unlock = new PageUnlockCondition();

        public RectTransform GroupParent => groupParent != null ? groupParent : (RectTransform)transform;
        public PageUnlockCondition Unlock => unlock;
        public bool IsUnlocked => unlock == null || unlock.IsMet;
        public string Requirement => unlock != null ? unlock.Describe() : string.Empty;

        // Called by the editor build step to inject the parsed condition.
        public void ConfigureUnlock(PageUnlockCondition condition) => unlock = condition;

        public void RefreshLockState()
        {
            bool unlocked = IsUnlocked;
            if (unlockedRoot != null) unlockedRoot.SetActive(unlocked);
            if (lockedRoot != null) lockedRoot.SetActive(!unlocked);
            if (!unlocked && lockedLabel != null)
                lockedLabel.text = Requirement;
        }

        // Show/hide the whole page (called by the pager when switching tabs).
        public void SetSelected(bool selected)
        {
            gameObject.SetActive(selected);
            if (selected) RefreshLockState();
        }
    }
}
