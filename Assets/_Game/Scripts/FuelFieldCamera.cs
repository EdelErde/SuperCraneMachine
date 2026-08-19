using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// Drives the offscreen "field" camera in the metaball pipeline. This camera
    /// sees ONLY the FuelField layer (the soft-circle droplet sprites), renders them
    /// into a RenderTexture, and that texture is then thresholded by the composite
    /// material to produce the merged liquid silhouette.
    ///
    /// Attach to a Camera. That camera should:
    ///   - have Culling Mask = FuelField layer only
    ///   - match the main camera's orthographic size & position (so the field lines
    ///     up 1:1 with the world), or be a child of the main camera
    ///   - render into the assigned RenderTexture
    ///
    /// This script keeps the field camera framed on the same world region as a
    /// reference camera (usually Camera.main) and (re)creates the RenderTexture with
    /// a valid depth/stencil buffer, which Unity 6's Render Graph requires.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FuelFieldCamera : MonoBehaviour
    {
        [Tooltip("Camera whose framing this field camera should copy. Defaults to Camera.main.")]
        [SerializeField] private Camera referenceCamera;

        [Tooltip("RenderTexture the field is drawn into. If left empty one is created at runtime.")]
        [SerializeField] private RenderTexture fieldTexture;

        [Header("Runtime-created texture settings (used only if fieldTexture is empty)")]
        [SerializeField] private int width = 512;
        [SerializeField] private int height = 256;

        [Tooltip("Keep this field camera aligned to the reference camera every frame " +
                 "(size + position). Turn off if you position it manually.")]
        [SerializeField] private bool followReference = true;

        private Camera _cam;
        private bool _createdTexture;

        public RenderTexture FieldTexture => fieldTexture;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (referenceCamera == null) referenceCamera = Camera.main;

            EnsureTexture();
            _cam.targetTexture = fieldTexture;
            _cam.orthographic = true;

            // Field should be clear each frame; additive droplet sprites accumulate on top.
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        }

        private void EnsureTexture()
        {
            if (fieldTexture != null) return;

            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0)
            {
                // Explicit depth/stencil format — the legacy int depth param does not
                // reliably persist under Unity 6 Render Graph, so set it directly.
                depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None,
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            fieldTexture = new RenderTexture(desc) { name = "FuelFieldTexture_Runtime" };
            fieldTexture.Create();
            _createdTexture = true;
        }

        private void OnDestroy()
        {
            if (_createdTexture && fieldTexture != null)
            {
                fieldTexture.Release();
                Destroy(fieldTexture);
            }
        }

        private void LateUpdate()
        {
            if (!followReference || referenceCamera == null) return;

            // Match framing so field pixels map to the same world space the player sees.
            _cam.orthographicSize = referenceCamera.orthographicSize;
            var refPos = referenceCamera.transform.position;
            transform.position = new Vector3(refPos.x, refPos.y, transform.position.z);
        }
    }
}
