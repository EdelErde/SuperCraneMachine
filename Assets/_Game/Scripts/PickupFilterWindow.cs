using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraneMachine
{
    // Screen-space panel listing every unlocked item type as a clickable icon.
    // Opened/closed with the right mouse button (press RMB again to close).
    // Click an icon to block/unblock that item type from the mouse drag-pickup
    // mechanic (see PickupFilterService / WorldInteractionController).
    //
    // Generation mirrors SortingConfigWindow's old approach: rebuilt from the
    // ItemDatabase each time it's opened, so newly-unlocked items just appear.
    public class PickupFilterWindow : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform iconParent;
        [SerializeField] private PickupFilterIcon iconPrefab;

        [Tooltip("Source of item types to list. Falls back to the spawner's database.")]
        [SerializeField] private ItemDatabase database;

        [Tooltip("If true, only list currently-unlocked item types.")]
        [SerializeField] private bool unlockedOnly = true;

        private readonly List<PickupFilterIcon> _icons = new List<PickupFilterIcon>();
        private bool _open;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        private void Update()
        {
            if (RightMousePressedThisFrame())
                SetOpen(!_open);
        }

        private void SetOpen(bool open)
        {
            _open = open;
            if (root != null) root.SetActive(open);
            if (open) Rebuild();
        }

        private ItemDatabase ResolveDatabase()
        {
            if (database != null) return database;
            return ServiceLocator.ItemSpawner != null ? ServiceLocator.ItemSpawner.Database : null;
        }

        private void Rebuild()
        {
            ClearIcons();
            if (iconPrefab == null || iconParent == null) return;

            var db = ResolveDatabase();
            if (db == null) return;

            foreach (var prefab in db.Prefabs)
            {
                if (prefab == null) continue;
                var item = prefab.GetComponent<Item>();
                if (item == null || item.type == null) continue;
                if (unlockedOnly && !item.type.Unlocked) continue;

                var icon = Instantiate(iconPrefab, iconParent);
                icon.Bind(item.type, SpriteOf(prefab));
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

#if ENABLE_INPUT_SYSTEM
        private static bool RightMousePressedThisFrame() =>
            Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
        private static bool RightMousePressedThisFrame() => Input.GetMouseButtonDown(1);
#endif
    }
}