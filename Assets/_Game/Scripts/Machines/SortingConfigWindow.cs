using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // Runtime window for configuring a SortingMachine's routing.
    // The player picks item types and a ratio (0..1) that should go to hole B; the rest
    // goes to hole A. One row per item type is spawned from a simple row prefab.
    //
    // KISS: a single shared window; Open(machine) rebinds it to whichever machine was clicked.
    public class SortingConfigWindow : MonoBehaviour
    {
        public static SortingConfigWindow Instance { get; private set; }

        [Header("Wiring")]
        [SerializeField] private GameObject root;               // panel to show/hide
        [SerializeField] private RectTransform rowParent;       // where rows spawn
        [SerializeField] private SortingConfigRow rowPrefab;    // row: label + slider + value
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private Button closeButton;

        [Tooltip("Source of item types to list. Falls back to the spawner's database.")]
        [SerializeField] private ItemDatabase database;

        [Tooltip("If true, only list currently-unlocked item types.")]
        [SerializeField] private bool unlockedOnly = true;

        private readonly List<SortingConfigRow> _rows = new List<SortingConfigRow>();
        private SortingMachine _machine;

        private void Awake()
        {
            Instance = this;
            if (root != null) root.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        public void Open(SortingMachine machine)
        {
            _machine = machine;
            if (root != null) root.SetActive(true);
            if (titleLabel != null) titleLabel.text = "Sorting  →  Hole B";
            Rebuild();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
            _machine = null;
        }

        private ItemDatabase ResolveDatabase()
        {
            if (database != null) return database;
            return ServiceLocator.ItemSpawner != null ? ServiceLocator.ItemSpawner.Database : null;
        }

        private void Rebuild()
        {
            ClearRows();
            if (_machine == null || rowPrefab == null || rowParent == null) return;

            var db = ResolveDatabase();
            if (db == null) return;

            foreach (var prefab in db.Prefabs)
            {
                if (prefab == null) continue;
                var item = prefab.GetComponent<Item>();
                if (item == null || item.type == null) continue;
                if (unlockedOnly && !item.type.Unlocked) continue;

                var type = item.type;
                var rule = _machine.Config.GetRule(type.GetType());
                float ratio = rule != null ? rule.ratioToB : 0f;
                var sprite = SpriteOf(prefab);

                var row = Instantiate(rowPrefab, rowParent);
                row.Bind(type, ratio, value => _machine.Config.SetRatio(type, value), sprite);
                _rows.Add(row);
            }
        }

        // Pull the item icon off the prefab: UI Image first, then a world SpriteRenderer.
        // Mirrors ItemDatabase's own sprite lookup so rows match the rest of the UI.
        private static Sprite SpriteOf(GameObject prefab)
        {
            if (prefab == null) return null;
            var img = prefab.GetComponentInChildren<Image>(true);
            if (img != null && img.sprite != null) return img.sprite;
            var sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
            return sr != null ? sr.sprite : null;
        }

        private void ClearRows()
        {
            foreach (var r in _rows)
                if (r != null) Destroy(r.gameObject);
            _rows.Clear();
        }
    }
}
