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

    public class UpgradeView : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private UpgradeGroup groupPrefab;
        [SerializeField] private UpgradeButton buttonPrefab;

        [Header("Where groups are created")]
        [SerializeField] private RectTransform content;

        [Header("Setup script")]
        [TextArea(6, 30)]
        [SerializeField] private string setupScript = "";

        [Header("Groups")]
        [SerializeField] private List<UpgradeGroupDefinition> groups = new List<UpgradeGroupDefinition>();

        public UpgradeGroup GroupPrefab => groupPrefab;
        public UpgradeButton ButtonPrefab => buttonPrefab;
        public RectTransform Content => content;
        public string SetupScript => setupScript;
        public List<UpgradeGroupDefinition> Groups => groups;
    }
}