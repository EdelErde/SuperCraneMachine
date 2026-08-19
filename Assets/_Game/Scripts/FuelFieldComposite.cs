using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// The VISIBLE liquid. Sits on a quad/SpriteRenderer in the normal scene (on a
    /// normal visible layer) that covers the same world region the field camera
    /// frames. Its material uses the FuelLiquid/Composite shader, which reads the
    /// field RenderTexture, thresholds it, and shades the result into the merged
    /// liquid look.
    ///
    /// This binder just pushes the field texture (and optional live tuning values)
    /// into that material each frame so you can tweak in the inspector at runtime.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class FuelFieldComposite : MonoBehaviour
    {
        [SerializeField] private FuelFieldCamera fieldCamera;

        [Header("Look (pushed to the composite material)")]
        [Range(0f, 1f)]
        [Tooltip("Field value above which pixels become solid liquid. Higher = smaller, " +
                 "tighter blobs; lower = they merge into a bigger mass more eagerly.")]
        [SerializeField] private float threshold = 0.5f;

        [Range(0.001f, 0.5f)]
        [Tooltip("Width of the anti-aliased edge around the threshold.")]
        [SerializeField] private float edgeSoftness = 0.05f;

        [SerializeField] private Color liquidColor = new Color(0.95f, 0.75f, 0.15f, 1f);

        private static readonly int FieldTexID = Shader.PropertyToID("_FieldTex");
        private static readonly int ThresholdID = Shader.PropertyToID("_Threshold");
        private static readonly int EdgeID = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int ColorID = Shader.PropertyToID("_LiquidColor");

        private Material _mat;

        private void Awake()
        {
            _mat = GetComponent<Renderer>().material; // instance, safe to write per-object
            if (fieldCamera == null) fieldCamera = FindFirstObjectByType<FuelFieldCamera>();
        }

        private void LateUpdate()
        {
            if (_mat == null) return;

            if (fieldCamera != null && fieldCamera.FieldTexture != null)
                _mat.SetTexture(FieldTexID, fieldCamera.FieldTexture);

            _mat.SetFloat(ThresholdID, threshold);
            _mat.SetFloat(EdgeID, edgeSoftness);
            _mat.SetColor(ColorID, liquidColor);
        }
    }
}
