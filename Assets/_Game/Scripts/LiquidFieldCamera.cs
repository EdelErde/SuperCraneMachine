using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// Offscreen field camera. Renders ONLY the Liquid layer (the soft-circle
    /// droplet sprites) into a RenderTexture. Same role as Code Monkey's second
    /// camera that targets FluidRenderTexture.
    ///
    /// Unlike his fixed-camera demo, this game's camera MOVES and switches screens,
    /// so this camera parents itself to the reference (main) camera and copies its
    /// orthographic size every frame — the field therefore always frames exactly the
    /// same world region the player sees. That is what makes the effect stay aligned
    /// across screen switches (the bug in the first attempt).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class LiquidFieldCamera : MonoBehaviour
    {
        [SerializeField] private Camera referenceCamera;
        [SerializeField] private RenderTexture fieldTexture;

        private Camera _cam;

        public RenderTexture FieldTexture => fieldTexture;
        public Camera ReferenceCamera => referenceCamera;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (referenceCamera == null) referenceCamera = Camera.main;

            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparent, additive on top
            _cam.targetTexture = fieldTexture;

            AlignToReference();
        }

        private void LateUpdate()
        {
            AlignToReference();
        }

        private void AlignToReference()
        {
            if (referenceCamera == null) return;
            _cam.orthographicSize = referenceCamera.orthographicSize;

            // Match the reference camera's ASPECT, not just its size. The field
            // RenderTexture is a fixed 512x256 (aspect 2.0), but the game camera renders
            // to the screen at whatever aspect the window is (~1.78 for 16:9). Left to
            // its own devices the field camera would project at the RT's 2.0 aspect and
            // therefore show MORE horizontal world than the screen does, so the composited
            // liquid ends up horizontally offset/compressed relative to what the player
            // sees. Forcing _cam.aspect to the reference camera makes both project the
            // same world region; the RT then just stores that view (with a little
            // horizontal padding), and the composite quad (also sized by the reference
            // camera's aspect) samples it 1:1.
            _cam.aspect = referenceCamera.aspect;

            var p = referenceCamera.transform.position;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }
    }
}