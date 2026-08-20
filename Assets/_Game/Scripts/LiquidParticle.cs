using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// One physics-driven droplet of liquid. Same physics role as Code Monkey's
    /// LiquidParticle (Rigidbody2D + CircleCollider2D so droplets fall and pile), but
    /// carries a LiquidType + color so a SINGLE shared field can render many liquids at
    /// once, tinted per droplet. Does NOT touch transform scale — prefab scale is
    /// authoritative.
    ///
    /// The SpriteRenderer uses the shared blob material via a MaterialPropertyBlock so
    /// each droplet can push its own color without creating a material instance per drop.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class LiquidParticle : MonoBehaviour
    {
        [Tooltip("Scales ONLY the sprite (visual reach), not the collider. This is the " +
                 "Code Monkey trick that fuses droplets: the soft sprite is made much " +
                 "bigger than the physics body so packed droplets overlap heavily and the " +
                 "threshold merges them into one mass. <=0 leaves the prefab scale as-is.")]
        [SerializeField] private float visualScale = 2.5f;

        [Tooltip("Optional lifetime in seconds before the droplet despawns. <=0 = never.")]
        [SerializeField] private float lifetime = 0f;

        private Rigidbody2D _rb;
        private SpriteRenderer _renderer;
        private CircleCollider2D _collider;
        private MaterialPropertyBlock _mpb;
        private float _spawnTime;

        private static readonly int ColorID = Shader.PropertyToID("_Color");

        public Rigidbody2D Body => _rb;
        public LiquidType Type { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            _mpb = new MaterialPropertyBlock();
            ApplyVisualScale();
        }

        // Grow the sprite relative to the collider so droplets that are physically
        // touching (collider size) have massively overlapping soft gradients (sprite
        // size). This does NOT change physics — the collider is untouched.
        private void ApplyVisualScale()
        {
            if (visualScale <= 0f || _renderer == null || _renderer.sprite == null || _collider == null)
                return;

            float spriteWorldSize = _renderer.sprite.bounds.size.x; // world units at scale 1
            if (spriteWorldSize <= 0.0001f) return;

            float targetDiameter = _collider.radius * 2f * visualScale;
            float s = targetDiameter / spriteWorldSize;
            transform.localScale = new Vector3(s, s, 1f);
        }

        private void OnEnable()
        {
            _spawnTime = Time.time;
            LiquidFieldSystem.Register(this);
        }

        private void OnDisable()
        {
            LiquidFieldSystem.Unregister(this);
        }

        private void Update()
        {
            if (lifetime > 0f && Time.time - _spawnTime >= lifetime)
                LiquidFieldSystem.Despawn(this);
        }

        /// <summary>
        /// Configure this droplet for a liquid type: set its tint (into the sprite via a
        /// property block) and its physics feel (gravity/drag), then launch it.
        /// </summary>
        public void Configure(LiquidType type, LiquidConfig config, Vector2 velocity)
        {
            Type = type;

            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            // Push per-droplet color without instancing the shared material.
            _renderer.GetPropertyBlock(_mpb);
            Color c = config != null ? config.color : Color.white;
            c.a *= config != null ? config.intensity : 1f;
            _mpb.SetColor(ColorID, c);
            _renderer.SetPropertyBlock(_mpb);

            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (config != null)
            {
                _rb.gravityScale = config.gravityScale;
                _rb.linearDamping = config.linearDrag;
            }
            _rb.linearVelocity = velocity;
        }
    }
}