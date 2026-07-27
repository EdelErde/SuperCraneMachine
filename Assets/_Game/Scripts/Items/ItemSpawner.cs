using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class ItemSpawner : MonoBehaviour
    {
        public ItemDatabase Database => database;
        [SerializeField] private ItemDatabase database;
        [SerializeField] private BoxCollider2D spawnArea;

        [Header("Rain settings")]
        [SerializeField] private SpawnerConfig config = new SpawnerConfig();

        private float _timer;
        private float _cycle = 1f;
        private readonly List<Item> _live = new List<Item>();

        public event System.Action OnSpawned;

        public float SpawnProgress =>
            _cycle <= 0.001f ? 0f : Mathf.Clamp01(1f - (_timer / _cycle));

        public float TimeToNextSpawn => Mathf.Max(0f, _timer);
        public bool Paused => MaxLiveValue > 0 && _live.Count >= MaxLiveValue;

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
            _cycle = NextInterval();
            _timer = _cycle;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _cycle = NextInterval();
            _timer = _cycle;

            if (AtCap()) return;
            for (int i = 0; i < itemsPerDrop && !AtCap(); i++)
                SpawnOne();

            OnSpawned?.Invoke();
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

        public int LiveCount
        {
            get
            {
                _live.RemoveAll(i => i == null);
                return _live.Count;
            }
        }

        public int MaxCount => MaxLiveValue;

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