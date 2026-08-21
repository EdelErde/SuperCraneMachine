using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // World-space setup window for one DroneFab. Same interaction as the Sorter Dialog:
    // every unlocked item type is a draggable icon; you drag an icon into a destination
    // column to route that item type there. Assigning a new column replaces the old one
    // (one destination per type). There's always an "Unassigned" column (drag back to
    // stop drones carrying that type).
    //
    // Difference from the Sorter Dialog: the columns are DYNAMIC — one per DroneDestination
    // in the scene — so the controller builds the columns at open time from
    // DroneDestination.All, then fills them from the fab's DroneRouteConfig.
    public class DroneSetupController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject root;
        [SerializeField] private DroneFab fab;

        [Header("Columns")]
        [Tooltip("Parent the generated destination columns are placed under (e.g. a HorizontalLayoutGroup).")]
        [SerializeField] private RectTransform columnParent;
        [Tooltip("Prefab for one destination column (has a DroneSetupDropZone + a list area + a label).")]
        [SerializeField] private DroneSetupDropZone columnPrefab;
        [Tooltip("The always-present 'Unassigned' drop zone (place in the window; not generated).")]
        [SerializeField] private DroneSetupDropZone unassignedZone;

        [Header("Icons")]
        [SerializeField] private DroneSetupIcon iconPrefab;

        [Tooltip("Source of item types to list. Falls back to the spawner's database.")]
        [SerializeField] private ItemDatabase database;
        [Tooltip("If true, only list currently-unlocked item types.")]
        [SerializeField] private bool unlockedOnly = true;

        private readonly List<DroneSetupIcon> _icons = new List<DroneSetupIcon>();
        private readonly List<DroneSetupDropZone> _columns = new List<DroneSetupDropZone>();
        private bool _open;

        public bool IsOpen => _open;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        public void Toggle() => SetOpen(!_open);
        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);

        private void SetOpen(bool open)
        {
            _open = open;
            if (root != null) root.SetActive(open);
            if (open) Rebuild();
        }

        // Called by a drop zone when an icon lands on it. destinationId == "" for the
        // Unassigned column.
        public void HandleIconDropped(DroneSetupIcon icon, string destinationId)
        {
            if (icon == null || icon.Type == null || fab == null) return;
            fab.Config.SetDestination(icon.Type, destinationId);
        }

        private ItemDatabase ResolveDatabase()
        {
            if (database != null) return database;
            return ServiceLocator.ItemSpawner != null ? ServiceLocator.ItemSpawner.Database : null;
        }

        private void Rebuild()
        {
            ClearIcons();
            ClearColumns();
            if (fab == null || iconPrefab == null) return;

            BuildColumns();

            var db = ResolveDatabase();
            if (db == null) return;

            foreach (var prefab in db.Prefabs)
            {
                if (prefab == null) continue;
                var item = prefab.GetComponent<Item>();
                if (item == null || item.type == null) continue;
                if (unlockedOnly && !item.type.Unlocked) continue;

                string destId = fab.Config.DestinationIdFor(item.type.GetType());
                var zone = ResolveZone(destId);
                if (zone == null) zone = unassignedZone;   // destination went missing
                if (zone == null) continue;

                var icon = Instantiate(iconPrefab, zone.ListParent);
                icon.Bind(item.type, SpriteOf(prefab));
                _icons.Add(icon);
            }
        }

        // One generated column per live DroneDestination, plus the fixed Unassigned zone.
        private void BuildColumns()
        {
            if (columnPrefab == null || columnParent == null) return;

            foreach (var dest in DroneDestination.All)
            {
                if (dest == null) continue;
                var col = Instantiate(columnPrefab, columnParent);
                col.Configure(this, dest.Id, dest.DisplayName);
                _columns.Add(col);
            }

            if (unassignedZone != null)
                unassignedZone.Configure(this, "", "Unassigned");
        }

        private DroneSetupDropZone ResolveZone(string destinationId)
        {
            if (string.IsNullOrEmpty(destinationId)) return unassignedZone;
            for (int i = 0; i < _columns.Count; i++)
                if (_columns[i] != null && _columns[i].DestinationId == destinationId)
                    return _columns[i];
            return null;
        }

        private static Sprite SpriteOf(GameObject prefab)
        {
            if (prefab == null) return null;
            var img = prefab.GetComponentInChildren<Image>(true);
            if (img != null && img.sprite != null) return img.sprite;
            var sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
            return sr != null ? sr.sprite : null;
        }

        private void ClearIcons()
        {
            foreach (var i in _icons)
                if (i != null) Destroy(i.gameObject);
            _icons.Clear();
        }

        private void ClearColumns()
        {
            foreach (var c in _columns)
                if (c != null) Destroy(c.gameObject);
            _columns.Clear();
        }
    }
}