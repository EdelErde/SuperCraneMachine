using UnityEngine;

namespace MetaballLiquid2D
{
    /// <summary>
    /// Attach to the offscreen "Metaball Camera". This camera should render
    /// ONLY the "Liquid" layer (blob sprites using MetaballBlob.shader) into
    /// a private RenderTexture. Nothing this camera sees is ever shown to the
    /// player directly - MetaballComposite reads the resulting texture and
    /// draws the actual liquid shape into the visible scene.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class MetaballFieldCamera : MonoBehaviour
    {
        [Tooltip("Layer the metaball camera renders. Only blob sprites should be on this layer.")]
        public LayerMask liquidLayer;

        [Tooltip("Resolution of the field render texture. Lower = cheaper but blobbier/softer edges.")]
        public int textureWidth = 512;
        public int textureHeight = 512;

        [Tooltip("Recreate the texture automatically if Width/Height are changed at runtime in the inspector.")]
        public bool autoResize = false;

        /// <summary>The live render texture blobs are drawn into. Bind this to your composite material.</summary>
        public RenderTexture FieldTexture { get; private set; }

        Camera _camera;

        void OnEnable()
        {
            _camera = GetComponent<Camera>();
            CreateTexture();
        }

        void OnDisable()
        {
            ReleaseTexture();
        }

        void CreateTexture()
        {
            ReleaseTexture();

            // Single-channel float format gives smooth gradients (no 8-bit
            // banding on the threshold edge). Falls back if unsupported.
            RenderTextureFormat format = RenderTextureFormat.RHalf;
            if (!SystemInfo.SupportsRenderTextureFormat(format))
            {
                format = RenderTextureFormat.ARGB32;
            }

            FieldTexture = new RenderTexture(Mathf.Max(4, textureWidth), Mathf.Max(4, textureHeight), 0, format)
            {
                name = "MetaballFieldRT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            FieldTexture.Create();

            _camera.targetTexture = FieldTexture;
            _camera.cullingMask = liquidLayer;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.orthographic = true;
            // Render before the main camera so the texture is fresh this frame.
            if (_camera.depth >= 0) _camera.depth = -10;
        }

        void ReleaseTexture()
        {
            if (FieldTexture != null)
            {
                if (_camera != null) _camera.targetTexture = null;
                FieldTexture.Release();
                FieldTexture = null;
            }
        }

        void Update()
        {
            if (autoResize && FieldTexture != null &&
                (FieldTexture.width != textureWidth || FieldTexture.height != textureHeight))
            {
                CreateTexture();
            }
        }
    }
}
