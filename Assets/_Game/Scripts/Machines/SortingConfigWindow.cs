using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    public class SortingConfigWindow : MonoBehaviour
    {
        public static SortingConfigWindow Instance { get; private set; }

        [Header("Wiring")]
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform rowParent; 
        [SerializeField] private SortingConfigRow rowPrefab;
        [SerializeField] private TextMeshProUGUI titleLabel;

        [Tooltip("Source of item types to list. Falls back to the spawner's database.")]
        [SerializeField] private ItemDatabase database;

        [Tooltip("If true, only list currently-unlocked item types.")]
        [SerializeField] private bool unlockedOnly = true;

        private readonly List<SortingConfigRow> _rows = new List<SortingConfigRow>();
        [SerializeField] private SortingMachine _machine;

        private void Awake()
        {
            Instance = this;
            if (root != null) root.SetActive(false);
        }

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
