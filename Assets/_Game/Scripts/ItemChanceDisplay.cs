using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class ItemChanceDisplay : MonoBehaviour
    {
        [Header("Rows")]
        [Tooltip("Parent the rows spawn under. Give it a layout group.")]
        [SerializeField] private RectTransform rowContainer;
        [Tooltip("Row prefab. Must have an ItemIcon component.")]
        [SerializeField] private GameObject rowPrefab;

        [Header("Format")]
        [SerializeField] private string chanceFormat = "{0:0.#}%";
        [Tooltip("How often to check for changes. Rebuilds only when chances actually differ.")]
        [SerializeField] private float refreshInterval = 0.5f;
        [SerializeField] private bool refreshOnUpgrade = true;

        private readonly List<GameObject> _pool = new List<GameObject>();
        private float _timer;
        private int _lastHash;

        private void Start()
        {
            if (refreshOnUpgrade && ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged += ForceRebuild;
            ForceRebuild();
        }

        private void OnDestroy()
        {
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged -= ForceRebuild;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = refreshInterval;
            RebuildIfChanged();
        }

        private void ForceRebuild()
        {
            _lastHash = 0;
            RebuildIfChanged();
        }

        private void RebuildIfChanged()
        {
            var spawner = ServiceLocator.ItemSpawner;
            if (spawner == null || spawner.Database == null || rowContainer == null || rowPrefab == null)
                return;

            var chances = spawner.Database.GetSpawnChances();

            int hash = ComputeHash(chances);
            if (hash == _lastHash) return;
            _lastHash = hash;

            EnsurePool(chances.Count);

            for (int i = 0; i < _pool.Count; i++)
            {
                bool used = i < chances.Count;
                _pool[i].SetActive(used);
                if (!used) continue;

                var (type, chance, sprite) = chances[i];
                var row = _pool[i].GetComponent<ItemIcon>();
                if (row != null)
                    row.Set(type.DisplayName, chance, chanceFormat, sprite);
            }
        }

        private static int ComputeHash(List<(ItemType type, float chance, Sprite sprite)> chances)
        {
            unchecked
            {
                int h = 17;
                foreach (var (type, chance, _) in chances)
                {
                    h = h * 31 + (type != null ? type.DisplayName.GetHashCode() : 0);
                    h = h * 31 + Mathf.RoundToInt(chance * 1000f);
                }
                return h;
            }
        }

        private void EnsurePool(int needed)
        {
            while (_pool.Count < needed)
                _pool.Add(Instantiate(rowPrefab, rowContainer));
        }
    }
}