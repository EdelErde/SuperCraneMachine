using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// Central manager + pool for liquid droplets of ANY LiquidType. One instance in the
    /// scene. Author each liquid's color/feel in the `liquids` list (no ScriptableObjects,
    /// like the SFX system). All liquids share ONE field and merge together; the color is
    /// carried per droplet, so e.g. fuel and water blobs blend where they touch.
    ///
    /// Spawn from anywhere:
    ///   LiquidFieldSystem.Spawn(LiquidType.Fuel, worldPos, velocity);
    /// </summary>
    public class LiquidFieldSystem : MonoBehaviour
    {
        public static LiquidFieldSystem Instance { get; private set; }

        [Header("Prefab")]
        [Tooltip("Droplet prefab: LiquidParticle + Rigidbody2D + CircleCollider2D + " +
                 "SpriteRenderer (soft circle) using the shared blob material, on the Liquid layer.")]
        [SerializeField] private LiquidParticle particlePrefab;

        [Header("Liquids (author each type here — no assets needed)")]
        [Tooltip("One entry per LiquidType. Fuel is the first liquid. Add more entries as " +
                 "you add enum values.")]
        [SerializeField]
        private List<LiquidConfig> liquids = new List<LiquidConfig>
        {
            new LiquidConfig { type = LiquidType.Fuel }
        };

        [Header("Pool")]
        [SerializeField] private int prewarm = 64;
        [SerializeField] private int maxLive = 400;

        private static readonly List<LiquidParticle> _live = new List<LiquidParticle>();
        private readonly Queue<LiquidParticle> _pool = new Queue<LiquidParticle>();
        private readonly Dictionary<LiquidType, LiquidConfig> _configByType =
            new Dictionary<LiquidType, LiquidConfig>();

        public static IReadOnlyList<LiquidParticle> Live => _live;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[LiquidFieldSystem] Multiple instances; disabling extra.", this);
                enabled = false;
                return;
            }
            Instance = this;
            RebuildConfigLookup();

            for (int i = 0; i < prewarm; i++)
            {
                var p = CreatePooled();
                if (p != null) _pool.Enqueue(p);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnValidate()
        {
            // Keep the lookup fresh when editing the list in the inspector at runtime.
            if (Application.isPlaying) RebuildConfigLookup();
        }

        private void RebuildConfigLookup()
        {
            _configByType.Clear();
            foreach (var c in liquids)
                if (c != null) _configByType[c.type] = c;
        }

        public LiquidConfig GetConfig(LiquidType type)
        {
            if (_configByType.TryGetValue(type, out var c)) return c;
            // Fallback so an unconfigured type still renders (white, default feel).
            return new LiquidConfig { type = type, color = Color.white };
        }

        private LiquidParticle CreatePooled()
        {
            if (particlePrefab == null)
            {
                Debug.LogError("[LiquidFieldSystem] particlePrefab not assigned.", this);
                return null;
            }
            var p = Instantiate(particlePrefab, transform);
            p.gameObject.SetActive(false);
            return p;
        }

        /// <summary>Emit a droplet of the given liquid type at a world position.</summary>
        public static LiquidParticle Spawn(LiquidType type, Vector2 worldPos, Vector2 velocity = default)
        {
            if (Instance == null)
            {
                Debug.LogWarning("[LiquidFieldSystem] No instance in scene; cannot Spawn.");
                return null;
            }
            return Instance.SpawnInternal(type, worldPos, velocity);
        }

        private LiquidParticle SpawnInternal(LiquidType type, Vector2 worldPos, Vector2 velocity)
        {
            if (_live.Count >= maxLive && _live.Count > 0)
                Despawn(_live[0]);

            LiquidParticle p = _pool.Count > 0 ? _pool.Dequeue() : CreatePooled();
            if (p == null) return null;

            p.transform.position = worldPos;
            p.transform.rotation = Quaternion.identity;
            p.gameObject.SetActive(true);            // OnEnable -> Register
            p.Configure(type, GetConfig(type), velocity);
            return p;
        }

        public static void Despawn(LiquidParticle p)
        {
            if (p == null || Instance == null) return;
            if (p.gameObject.activeSelf) p.gameObject.SetActive(false); // OnDisable -> Unregister
            Instance._pool.Enqueue(p);
        }

        internal static void Register(LiquidParticle p)
        {
            if (p != null && !_live.Contains(p)) _live.Add(p);
        }

        internal static void Unregister(LiquidParticle p)
        {
            if (p != null) _live.Remove(p);
        }
    }
}