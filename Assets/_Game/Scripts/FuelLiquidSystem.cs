using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// Central manager + object pool for fuel liquid droplets. One instance in the
    /// scene. Other code calls FuelLiquidSystem.Spawn(worldPos, velocity) to emit a
    /// droplet (e.g. FuelFilter when it produces fuel, or a spout dripping into a
    /// tank). Pooling keeps allocations flat even when hundreds of droplets are live.
    ///
    /// This mirrors Code Monkey's approach where liquid is just many little
    /// Rigidbody2D circles; the "liquid mass" look is produced downstream by the
    /// field camera + threshold shader, not here.
    /// </summary>
    public class FuelLiquidSystem : MonoBehaviour
    {
        public static FuelLiquidSystem Instance { get; private set; }

        [Header("Prefab")]
        [Tooltip("Prefab with FuelLiquidParticle + Rigidbody2D + CircleCollider2D + " +
                 "SpriteRenderer (soft circle), on the FuelField layer.")]
        [SerializeField] private FuelLiquidParticle particlePrefab;

        [Header("Pool")]
        [SerializeField] private int prewarm = 64;
        [Tooltip("Hard cap on live droplets. Oldest is recycled when exceeded so the " +
                 "sim never runs away on a big level.")]
        [SerializeField] private int maxLive = 400;

        // Active droplets, in spawn order (front = oldest) so we can recycle FIFO.
        private static readonly List<FuelLiquidParticle> _live = new List<FuelLiquidParticle>();
        private readonly Queue<FuelLiquidParticle> _pool = new Queue<FuelLiquidParticle>();

        public static IReadOnlyList<FuelLiquidParticle> Live => _live;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[FuelLiquidSystem] Multiple instances; disabling extra.", this);
                enabled = false;
                return;
            }
            Instance = this;

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

        private FuelLiquidParticle CreatePooled()
        {
            if (particlePrefab == null)
            {
                Debug.LogError("[FuelLiquidSystem] particlePrefab not assigned.", this);
                return null;
            }
            var p = Instantiate(particlePrefab, transform);
            p.gameObject.SetActive(false);
            return p;
        }

        /// <summary>Emit a droplet at a world position with an optional launch velocity.</summary>
        public static FuelLiquidParticle Spawn(Vector2 worldPos, Vector2 velocity = default)
        {
            if (Instance == null)
            {
                Debug.LogWarning("[FuelLiquidSystem] No instance in scene; cannot Spawn.");
                return null;
            }
            return Instance.SpawnInternal(worldPos, velocity);
        }

        private FuelLiquidParticle SpawnInternal(Vector2 worldPos, Vector2 velocity)
        {
            // Recycle oldest if we're at the cap.
            if (_live.Count >= maxLive && _live.Count > 0)
            {
                Despawn(_live[0]);
            }

            FuelLiquidParticle p = _pool.Count > 0 ? _pool.Dequeue() : CreatePooled();
            if (p == null) return null;

            p.transform.position = worldPos;
            p.transform.rotation = Quaternion.identity;
            p.gameObject.SetActive(true);   // OnEnable -> Register
            p.Launch(velocity);
            return p;
        }

        /// <summary>Return a droplet to the pool (deactivates it).</summary>
        public static void Despawn(FuelLiquidParticle p)
        {
            if (p == null || Instance == null) return;
            if (p.gameObject.activeSelf)
                p.gameObject.SetActive(false); // OnDisable -> Unregister
            Instance._pool.Enqueue(p);
        }

        // Called by particles from OnEnable/OnDisable so the live list stays accurate
        // even for droplets placed in the scene by hand rather than via Spawn().
        internal static void Register(FuelLiquidParticle p)
        {
            if (p != null && !_live.Contains(p)) _live.Add(p);
        }

        internal static void Unregister(FuelLiquidParticle p)
        {
            if (p != null) _live.Remove(p);
        }
    }
}
