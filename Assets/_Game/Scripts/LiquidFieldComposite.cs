using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// The VISIBLE liquid. A Quad (like Code Monkey's "FluidVisual") whose material
    /// reads the field RenderTexture and hard-thresholds it into a solid connected
    /// mass. In his demo the quad is a static 200x100 object at the origin and works
    /// only because his camera never moves. THIS version instead parents the quad to
    /// the field camera and resizes it every frame to exactly fill the orthographic
    /// view, so it stays glued to the screen when the game camera pans / switches
    /// screens.
    ///
    /// The quad is drawn by the MAIN camera (put it on a normal visible layer). The
    /// droplets themselves are on the Liquid layer, which the main camera must NOT
    /// render (set that up once — see the setup tool). So the player only ever sees
    /// this thresholded quad, never the raw circles.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class LiquidFieldComposite : MonoBehaviour
    {
        [SerializeField] private LiquidFieldCamera fieldCamera;

        [Header("Look")]
        [Range(0f, 1f)]
        [Tooltip("Field value the surface forms at. Lower = droplets merge more eagerly " +
                 "into one mass; higher = tighter separate blobs. ~0.25 reads as liquid.")]
        [SerializeField] private float threshold = 0.25f;

        [Range(0f, 0.3f)]
        [Tooltip("Tiny softness for edge anti-aliasing only. 0 = a razor-hard edge like a pure Step.")]
        [SerializeField] private float edgeSoftness = 0.02f;

        [Tooltip("Optional global tint multiplying ALL liquids. Leave white to show each " +
                 "droplet's own color unchanged. Per-liquid color is set in LiquidFieldSystem.")]
        [SerializeField] private Color globalTint = Color.white;

        [Tooltip("Push the quad this far in front of the camera (orthographic, so any positive z works).")]
        [SerializeField] private float distanceFromCamera = 1f;

        private static readonly int FieldTexID = Shader.PropertyToID("_FieldTex");
        private static readonly int ThresholdID = Shader.PropertyToID("_Threshold");
        private static readonly int EdgeID = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int TintID = Shader.PropertyToID("_Tint");

        private Material _mat;
        private Camera _refCam;

        private void Awake()
        {
            _mat = GetComponent<Renderer>().material;
            if (fieldCamera == null) fieldCamera = FindFirstObjectByType<LiquidFieldCamera>();
            _refCam = fieldCamera != null && fieldCamera.ReferenceCamera != null
                ? fieldCamera.ReferenceCamera
                : Camera.main;

            // Glue the quad to the camera so it always fills the view.
            if (_refCam != null)
                transform.SetParent(_refCam.transform, worldPositionStays: false);
        }

        private void LateUpdate()
        {
            if (_mat != null && fieldCamera != null && fieldCamera.FieldTexture != null)
            {
                _mat.SetTexture(FieldTexID, fieldCamera.FieldTexture);
                _mat.SetFloat(ThresholdID, threshold);
                _mat.SetFloat(EdgeID, edgeSoftness);
                _mat.SetColor(TintID, globalTint);
            }

            FitToCamera();
        }

        // Size the quad to exactly cover the orthographic frustum and sit centred in front.
        private void FitToCamera()
        {
            if (_refCam == null) return;
            float h = _refCam.orthographicSize * 2f;
            float w = h * _refCam.aspect;
            transform.localScale = new Vector3(w, h, 1f);
            transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
            transform.localRotation = Quaternion.identity;
        }
    }
}