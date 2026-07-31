using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    [Serializable]
    public class UpgradeEntry
    {
        [SerializeReference, UpgradeReference] public IUpgrade upgrade;

        [Header("Unlock gate (optional)")]
        [SerializeReference, UpgradeReference] public IUpgrade unlockedBy;
        [Min(1)] public int requiredLevel = 1;
    }

    [Serializable]
    public class UpgradeGroupDefinition
    {
        public string title = "Group";
        public List<UpgradeEntry> upgrades = new List<UpgradeEntry>();
    }

    // How a page becomes unlocked.
    public enum PageUnlockMode
    {
        Always,          // first page / no gate
        SpecificUpgrade, // a named upgrade must reach requiredLevel
        UpgradeCount,    // a total number of upgrade purchases across the game
    }

    [Serializable]
    public class UpgradePageDefinition
    {
        public string title = "Page";

        [Header("Unlock condition")]
        public PageUnlockMode unlockMode = PageUnlockMode.Always;

        [Tooltip("For SpecificUpgrade: the gating upgrade.")]
        [SerializeReference, UpgradeReference] public IUpgrade unlockedBy;
        [Tooltip("For SpecificUpgrade: level the gating upgrade must reach.")]
        [Min(1)] public int requiredLevel = 1;

        [Tooltip("For UpgradeCount: total upgrade purchases needed to unlock this page.")]
        [Min(1)] public int requiredUpgradeCount = 1;

        public List<UpgradeGroupDefinition> groups = new List<UpgradeGroupDefinition>();
    }

    public class UpgradeView : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private UpgradeGroup groupPrefab;
        [SerializeField] private UpgradeButton buttonPrefab;

        [Header("Where groups are created")]
        [SerializeField] private RectTransform content;

        [Header("Paging")]
        [Tooltip("Prefab for a page (holds its own group content area).")]
        [SerializeField] private UpgradePage pagePrefab;
        [Tooltip("Prefab for a page tab in the top row.")]
        [SerializeField] private UpgradePageTab tabPrefab;
        [Tooltip("Top row where page tabs are created.")]
        [SerializeField] private RectTransform tabBar;

        [Header("Setup script")]
        [TextArea(6, 30)]
        [SerializeField] private string setupScript = "";

        [Header("Pages")]
        [SerializeField] private List<UpgradePageDefinition> pages = new List<UpgradePageDefinition>();

        [Header("Legacy (single-page) groups")]
        [Tooltip("Used only when no pages are defined, for backward compatibility.")]
        [SerializeField] private List<UpgradeGroupDefinition> groups = new List<UpgradeGroupDefinition>();

        public UpgradeGroup GroupPrefab => groupPrefab;
        public UpgradeButton ButtonPrefab => buttonPrefab;
        public RectTransform Content => content;
        public string SetupScript => setupScript;
        public List<UpgradeGroupDefinition> Groups => groups;

        public UpgradePage PagePrefab => pagePrefab;
        public UpgradePageTab TabPrefab => tabPrefab;
        public RectTransform TabBar => tabBar;
        public List<UpgradePageDefinition> Pages => pages;

        [Header("Startup")]
        [Tooltip("Hide the whole view on startup (after initializing its buttons). The buy " +
                 "window is usually opened by a button, so it starts hidden.")]
        [SerializeField] private bool startHidden = true;

        // Initialize every child button up front (even inactive ones), so they register with
        // the UpgradeService immediately. This replaces the old 'stay active one frame so
        // Start() runs, then hide' coroutine: registration no longer depends on Unity firing
        // Start() on each button, so we can hide right away with no frame delay.
        private void Awake()
        {
            InitializeContents();
            if (startHidden)
                gameObject.SetActive(false);
        }

        private void InitializeContents()
        {
            // Buttons first: they register with the service so gate/preview relationships
            // resolve. Groups' visibility depends on their buttons' Visible state, so groups
            // must initialize AFTER buttons.
            var buttons = GetComponentsInChildren<UpgradeButton>(true);
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null) buttons[i].Initialize();

            // All buttons registered; resolve cross-button relationships once.
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.NotifyChanged();

            // Now initialize groups (and pages), which compute their visibility from buttons.
            var groups = GetComponentsInChildren<UpgradeGroup>(true);
            for (int i = 0; i < groups.Length; i++)
                if (groups[i] != null) groups[i].Initialize();

            // Finally the pager: it selects the first page and refreshes tabs, and depends on
            // buttons/groups above already being resolved. Last so the first open is complete.
            var pager = GetComponentInChildren<UpgradePager>(true);
            if (pager != null) pager.Initialize();
        }
    }
}