using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class ItemSpawner : MonoBehaviour
    {
        [SerializeField] private ItemDatabase database;
        [SerializeField] private BoxCollider2D spawnArea;

        [Header("Rain settings")]
        [SerializeField] private SpawnerConfig config = new SpawnerConfig();

        private float _timer;
        private readonly List<Item> _live = new List<Item>();

        private int itemsPerDrop => config.itemsPerDrop;
        private float intervalJitter => config.intervalJitter;
        private int initialBurst => config.initialBurst;
        private bool spawnOnStart => config.spawnOnStart;

        private float SpawnIntervalValue =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.SpawnInterval) : config.spawnInterval;
        private int MaxLiveValue =>
            ServiceLocator.StatService != null ? Mathf.RoundToInt(ServiceLocator.StatService.GameValue(GameStat.MaxLiveItems)) : config.maxLiveItems;

        private void Awake() => ServiceLocator.ItemSpawner = this;

        private void Start()
        {
            if (spawnOnStart) SpawnBatch(initialBurst);
            _timer = NextInterval();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _timer = NextInterval();

            if (AtCap()) return;
            for (int i = 0; i < itemsPerDrop && !AtCap(); i++)
                SpawnOne();
        }

        public void SpawnBatch(int count)
        {
            for (int i = 0; i < count && !AtCap(); i++) SpawnOne();
        }

        public void SpawnOne()
        {
            if (database == null || spawnArea == null) return;

            var prefab = database.PickRandom();
            if (prefab == null) return;

            var rot = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            var go = Instantiate(prefab, RandomPointInArea(), rot);

            var item = go.GetComponent<Item>();
            if (item != null) _live.Add(item);
        }

        private bool AtCap()
        {
            if (MaxLiveValue <= 0) return false;
            _live.RemoveAll(i => i == null);
            return _live.Count >= MaxLiveValue;
        }

        private float NextInterval()
        {
            return Mathf.Max(0.01f, SpawnIntervalValue + Random.Range(-intervalJitter, intervalJitter));
        }

        private Vector3 RandomPointInArea()
        {
            Bounds b = spawnArea.bounds;
            return new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                0f);
        }
    }
}