using UnityEngine;

namespace MetaballLiquid2D
{
    /// <summary>
    /// Optional helper for individual blob sprites. Lets you vary each
    /// blob's field strength/tint per-instance (e.g. bigger blobs merge more
    /// readily) via a MaterialPropertyBlock, without creating extra material
    /// instances.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class MetaballBlob : MonoBehaviour
    {
        [Tooltip("Per-instance field intensity multiplier. Higher = contributes more to the merge, and reaches threshold on its own from farther away.")]
        public float fieldIntensity = 1f;

        [Tooltip("Per-instance field tint. Usually leave white - the visible liquid color comes from the composite material, not this.")]
        public Color fieldColor = Color.white;

        static readonly int ColorID = Shader.PropertyToID("_Color");
        static readonly int IntensityID = Shader.PropertyToID("_Intensity");

        SpriteRenderer _sr;
        MaterialPropertyBlock _mpb;

        void OnEnable()
        {
            _sr = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
            Apply();
        }

        void OnValidate()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) Apply();
        }

        /// <summary>Call after changing fieldIntensity/fieldColor at runtime.</summary>
        public void Apply()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _sr.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorID, fieldColor);
            _mpb.SetFloat(IntensityID, fieldIntensity);
            _sr.SetPropertyBlock(_mpb);
        }

        public void SetIntensity(float value)
        {
            fieldIntensity = value;
            Apply();
        }
    }
}
