using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// Optional glue: emits fuel liquid droplets from a spout whenever the FuelFilter
    /// on the same object (or an assigned one) fires its OnProduce event. Additive —
    /// drop this onto the FuelFilter GameObject, no changes to FuelFilter.cs needed
    /// (it already exposes `public event System.Action OnProduce`).
    ///
    /// If you'd rather drive droplets from somewhere else (a manual drip, a tank
    /// overflow), skip this and call FuelLiquidSystem.Spawn(...) directly.
    /// </summary>
    public class FuelLiquidEmitter : MonoBehaviour
    {
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
                FuelLiquidSystem.Spawn(pos, vel);
            }
        }
    }
}
