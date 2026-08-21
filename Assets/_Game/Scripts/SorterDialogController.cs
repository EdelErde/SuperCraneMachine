using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // World-space dialog for one SortingMachine. Opened by clicking a button on the
    // machine (see SorterDialogOpenButton), closed the same way or via a close button.
    // Shows two parallel lists — Hole A and Hole B — as draggable icons; dragging an
    // icon from one list to the other reassigns that item type's exit on the machine's
    // SortingConfig. Starts with everything in Hole A (SortingConfig's default).
    public class SorterDialogController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject root;
        [SerializeField] private SortingMachine machine;

        [Header("Lists")]
        [SerializeField] private SorterDialogDropZone holeAZone;
        [SerializeField] private SorterDialogDropZone holeBZone;
        [Tooltip("The third-exit column. Its GameObject is shown only when the machine's exit " +
                 "C is unlocked; leave unassigned on two-exit sorters.")]
        [SerializeField] private SorterDialogDropZone holeCZone;
        [SerializeField] private SorterDialogIcon iconPrefab;

        [Tooltip("Source of item types to list. Falls back to the spawner's database.")]
        [SerializeField] private ItemDatabase database;

        [Tooltip("If true, only list currently-unlocked item types.")]
        [SerializeField] private bool unlockedOnly = true;

        private readonly List<SorterDialogIcon> _icons = new List<SorterDialogIcon>();
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

        private void OnEnable()
        {
            Rebuild();
        }

        // Called by SorterDialogDropZone when an icon is dropped on it.
        public void HandleIconDropped(SorterDialogIcon icon, SortExit exit)
        {
            if (icon == null || icon.Type == null || machine == null) return;
            machine.Config.SetExit(icon.Type, exit);
        }

        private ItemDatabase ResolveDatabase()
        {
            if (database != null) return database;
            return ServiceLocator.ItemSpawner != null ? ServiceLocator.ItemSpawner.Database : null;
        }

        private void Rebuild()
        {
            ClearIcons();
            if (iconPrefab == null || machine == null || holeAZone == null || holeBZone == null) return;

            // The third column only exists in the UI when this machine's exit C is unlocked.
            bool showC = holeCZone != null && machine.ExitCUnlocked;
            if (holeCZone != null) holeCZone.gameObject.SetActive(showC);

            var db = ResolveDatabase();
            if (db == null) return;

            foreach (var prefab in db.Prefabs)
            {
                if (prefab == null) continue;
                var item = prefab.GetComponent<Item>();
                if (item == null || item.type == null) continue;
                if (unlockedOnly && !item.type.Unlocked) continue;

                var type = item.type;
                var exit = machine.Config.ExitFor(type.GetType());

                // A type routed to C while C is hidden falls back to the A column visually
                // (its rule is left intact; SortingConfig.Decide also guards against routing
                // to a locked C at runtime).
                if (exit == SortExit.C && !showC) exit = SortExit.A;

                var zone = exit == SortExit.C ? holeCZone
                         : exit == SortExit.B ? holeBZone
                         : holeAZone;

                var icon = Instantiate(iconPrefab, zone.ListParent);
                icon.Bind(type, SpriteOf(prefab));
                _icons.Add(icon);
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

        private void ClearIcons()
        {
            foreach (var i in _icons)
                if (i != null) Destroy(i.gameObject);
            _icons.Clear();
        }
    }
}