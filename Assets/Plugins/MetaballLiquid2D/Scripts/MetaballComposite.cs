using UnityEngine;

namespace MetaballLiquid2D
{
    /// <summary>
    /// Attach to the quad the player actually sees (a standard Unity "Quad"
    /// primitive works). Binds the Metaball Camera's field texture into this
    /// object's MetaballComposite.shader material and, optionally, keeps the
    /// quad sized/positioned to exactly match the field camera's view.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class MetaballComposite : MonoBehaviour
    {
        [Tooltip("The camera that renders blobs into the field texture.")]
        public MetaballFieldCamera fieldCamera;

        [Tooltip("Automatically size/position this quad to exactly cover the field camera's orthographic view every frame. Turn off if you position the quad manually.")]
        public bool followFieldCamera = true;

        static readonly int FieldTexID = Shader.PropertyToID("_FieldTex");

        Renderer _renderer;
        MaterialPropertyBlock _mpb;

        void OnEnable()
        {
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            if (fieldCamera == null || fieldCamera.FieldTexture == null) return;

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetTexture(FieldTexID, fieldCamera.FieldTexture);
            _renderer.SetPropertyBlock(_mpb);

            if (followFieldCamera)
            {
                MatchFieldCamera();
            }
        }

        void MatchFieldCamera()
        {
            Camera cam = fieldCamera.GetComponent<Camera>();
            if (cam == null || !cam.orthographic) return;

            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;

            // Assumes a standard 1x1 unit Quad mesh.
            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);
            transform.localScale = new Vector3(width, height, 1f);
        }
    }
}
