using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "CraneMachine/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [Tooltip("The only thing to set: item prefabs. Each must have an Item component with a type selected.")]
        [SerializeField] private List<GameObject> prefabs = new List<GameObject>();

        public IReadOnlyList<GameObject> Prefabs => prefabs;

        // Current spawn chance (0-1) per unlocked item type, for UI.
        public List<(ItemType type, float chance, Sprite sprite)> GetSpawnChances()
        {
            var result = new List<(ItemType, float, Sprite)>();
            float total = 0f;

            foreach (var p in prefabs) total += WeightOf(p);
            if (total <= 0f) return result;

            foreach (var p in prefabs)
            {
                float w = WeightOf(p);
                if (w <= 0f) continue;
                var item = p.GetComponent<Item>();
                if (item != null && item.type != null)
                    result.Add((item.type, w / total, SpriteOf(p)));
            }

            result.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return result;
        }

        public GameObject PickRandom()
        {
            float total = 0f;
            foreach (var p in prefabs)
                total += WeightOf(p);

            if (total <= 0f) return null;

            float r = UnityEngine.Random.Range(0f, total);
            foreach (var p in prefabs)
            {
                r -= WeightOf(p);
                if (r <= 0f) return p;
            }
            return null;
        }

        private static float WeightOf(GameObject prefab)
        {
            if (prefab == null) return 0f;
            var item = prefab.GetComponent<Item>();
            if (item == null || item.type == null) return 0f;
            if (!item.type.Unlocked) return 0f;
            return Mathf.Max(0f, item.type.CurrentWeight);
        }

        // Pull the icon off the prefab: UI Image first (UI-based solution),
        // then a world SpriteRenderer as a fallback.
        private static Sprite SpriteOf(GameObject prefab)
        {
            if (prefab == null) return null;
            var img = prefab.GetComponentInChildren<Image>(true);
            if (img != null && img.sprite != null) return img.sprite;
            var sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
            return sr != null ? sr.sprite : null;
        }

        public GameObject Find(Type itemType)
        {
            foreach (var p in prefabs)
            {
                if (p == null) continue;
                var item = p.GetComponent<Item>();
                if (item != null && item.type != null && item.type.GetType() == itemType)
                    return p;
            }
            return null;
        }

        public GameObject Find<T>() where T : ItemType => Find(typeof(T));
    }
}