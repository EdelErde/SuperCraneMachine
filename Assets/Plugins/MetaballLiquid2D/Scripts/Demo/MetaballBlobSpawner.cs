using UnityEngine;

namespace MetaballLiquid2D.Demo
{
    /// <summary>
    /// Quick way to see the effect working: spawns a handful of blob sprites
    /// that gently wander around, so nearby ones merge and separating ones
    /// pinch apart. Not required for the effect itself - just a test rig.
    /// </summary>
    public class MetaballBlobSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [Tooltip("A sprite prefab using the M_MetaballBlob material, on the 'Liquid' layer (ideally with a MetaballBlob component too).")]
        public GameObject blobPrefab;

        [Header("Spawn")]
        public int blobCount = 12;
        public Vector2 spawnAreaSize = new Vector2(6f, 4f);
        public Vector2 scaleRange = new Vector2(0.8f, 1.6f);

        [Header("Movement")]
        public float wanderSpeed = 1.2f;
        public float wanderStrength = 1.5f;

        GameObject[] _blobs;
        Vector2[] _seeds;

        void Start()
        {
            if (blobPrefab == null)
            {
                Debug.LogError("MetaballBlobSpawner: assign a blob prefab first.");
                enabled = false;
                return;
            }

            _blobs = new GameObject[blobCount];
            _seeds = new Vector2[blobCount];

            for (int i = 0; i < blobCount; i++)
            {
                Vector2 pos = new Vector2(
                    Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
                    Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f));

                GameObject blob = Instantiate(blobPrefab, pos, Quaternion.identity, transform);
                float scale = Random.Range(scaleRange.x, scaleRange.y);
                blob.transform.localScale = Vector3.one * scale;

                _blobs[i] = blob;
                _seeds[i] = new Vector2(Random.value * 100f, Random.value * 100f);
            }
        }

        void Update()
        {
            if (_blobs == null) return;

            float t = Time.time * wanderSpeed;
            for (int i = 0; i < _blobs.Length; i++)
            {
                if (_blobs[i] == null) continue;

                Vector2 seed = _seeds[i];
                float dx = (Mathf.PerlinNoise(seed.x, t) - 0.5f) * 2f;
                float dy = (Mathf.PerlinNoise(seed.y, t) - 0.5f) * 2f;
                Vector2 drift = new Vector2(dx, dy) * wanderStrength * Time.deltaTime;

                Vector3 pos = _blobs[i].transform.position + (Vector3)drift;
                pos.x = Mathf.Clamp(pos.x, -spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f);
                pos.y = Mathf.Clamp(pos.y, -spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f);

                _blobs[i].transform.position = pos;
            }
        }
    }
}
