using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// Optional glue: emits droplets of a chosen LiquidType from a spout whenever the
    /// FuelFilter on the same object fires OnProduce. Additive — no changes to
    /// FuelFilter.cs needed. For non-fuel sources (a water outlet, an acid drip), reuse
    /// this component with a different `liquid`, or call LiquidFieldSystem.Spawn directly.
    /// </summary>
    public class LiquidEmitter : MonoBehaviour
    {
        [Tooltip("Which liquid this emitter sprays.")]
        [SerializeField] private LiquidType liquid = LiquidType.Fuel;

        [SerializeField] private FuelFilter fuelFilter;

        [Tooltip("Where droplets appear. Defaults to this transform if unset.")]
        [SerializeField] private Transform spout;

        [Header("Emission")]
        [SerializeField] private int dropletsPerProduce = 6;
        [SerializeField] private float spread = 0.15f;
        [SerializeField] private Vector2 launchVelocity = new Vector2(0f, -1.5f);
        [SerializeField] private float velocityJitter = 0.5f;

        private void Awake()
        {
            if (fuelFilter == null) fuelFilter = GetComponent<FuelFilter>();
            if (spout == null) spout = transform;
        }

        private void OnEnable()
        {
            if (fuelFilter != null) fuelFilter.OnProduce += HandleProduce;
        }

        private void OnDisable()
        {
            if (fuelFilter != null) fuelFilter.OnProduce -= HandleProduce;
        }

        private void HandleProduce()
        {
            Vector2 origin = spout != null ? (Vector2)spout.position : (Vector2)transform.position;
            for (int i = 0; i < dropletsPerProduce; i++)
            {
                Vector2 pos = origin + Random.insideUnitCircle * spread;
                Vector2 vel = launchVelocity + Random.insideUnitCircle * velocityJitter;
                LiquidFieldSystem.Spawn(liquid, pos, vel);
            }
        }
    }
}