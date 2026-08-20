using System;
using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// The kinds of liquid the shared field can render. Add new liquids here; Fuel is
    /// the first and default. A plain enum (no ScriptableObjects) so all authoring lives
    /// in one inspector list on LiquidFieldSystem, matching the project's centralized style.
    /// </summary>
    public enum LiquidType
    {
        Fuel = 0,
        // Add more as needed, e.g.:
        // Water = 1,
        // Acid = 2,
        // Lava = 3,
    }

    /// <summary>
    /// Per-liquid tuning, authored as a list entry on LiquidFieldSystem. All liquids
    /// share ONE field and merge together (color comes from the droplet), so the visual
    /// difference between liquids is just color + physics feel, not a separate field.
    /// </summary>
    [Serializable]
    public class LiquidConfig
    {
        [Tooltip("Which liquid this entry configures.")]
        public LiquidType type = LiquidType.Fuel;

        [Tooltip("Surface color of this liquid. Written per-droplet into the shared field.")]
        public Color color = new Color(0.95f, 0.75f, 0.15f, 1f);

        [Header("Physics feel (applied to spawned droplets of this type)")]
        [Tooltip("Rigidbody2D gravity scale for this liquid's droplets. Higher = falls faster / less floaty.")]
        public float gravityScale = 3f;

        [Tooltip("Linear drag on droplets. Higher = thicker/more viscous (oil), lower = runnier (water).")]
        public float linearDrag = 0f;

        [Tooltip("Optional per-liquid field strength. Higher merges more eagerly.")]
        public float intensity = 1f;
    }
}