using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// One physics-driven droplet of fuel liquid. Behaves like Code Monkey's
    /// LiquidParticle: a Rigidbody2D + CircleCollider2D so droplets fall and pile
    /// up realistically, plus a SpriteRenderer drawing a SOFT radial-gradient
    /// circle onto the offscreen fuel-field layer. The metaball "merge" look comes
    /// entirely from the field camera + threshold shader summing these soft
    /// circles — this script just supplies the physics body and the sprite.
    ///
    /// Put this on a prefab whose SpriteRenderer uses a soft circle sprite
    /// (Circle_Soft) and whose GameObject Layer is the FuelField layer.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class FuelLiquidParticle : MonoBehaviour
    {
        [Tooltip("Extra visual radius multiplier for the sprite vs the collider. " +
                 ">1 makes neighbouring droplets' soft circles overlap sooner, which " +
                 "is what lets the threshold shader fuse them into one mass.")]
        [SerializeField] private float visualScale = 2f;

        [Tooltip("Lifetime in seconds before the droplet despawns. <=0 = never.")]
        [SerializeField] private float lifetime = 0f;

        private Rigidbody2D _rb;
        private CircleCollider2D _collider;
        private SpriteRenderer _renderer;
        private float _spawnTime;

        public Rigidbody2D Body => _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();
            _renderer = GetComponent<SpriteRenderer>();

            // Scale the sprite up relative to the physical collider so the soft
            // gradients of adjacent droplets overlap while the solid bodies don't.
            if (_renderer != null && _collider != null)
            {
                float diameter = _collider.radius * 2f * visualScale;
                if (_renderer.sprite != null)
                {
                    float spriteWorldSize =
                        _renderer.sprite.bounds.size.x; // in world units at scale 1
                    if (spriteWorldSize > 0.0001f)
                    {
                        float s = diameter / spriteWorldSize;
                        transform.localScale = new Vector3(s, s, 1f);
                    }
                }
            }
        }

        private void OnEnable()
        {
            _spawnTime = Time.time;
            FuelLiquidSystem.Register(this);
        }

        private void OnDisable()
        {
            FuelLiquidSystem.Unregister(this);
        }

        private void Update()
        {
            if (lifetime > 0f && Time.time - _spawnTime >= lifetime)
            {
                FuelLiquidSystem.Despawn(this);
            }
        }

        /// <summary>Give the droplet an initial launch velocity (e.g. out of a spout).</summary>
        public void Launch(Vector2 velocity)
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            _rb.linearVelocity = velocity;
        }
    }
}
