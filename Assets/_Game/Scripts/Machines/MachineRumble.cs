using UnityEngine;

namespace CraneMachine
{
    // Small reusable "this machine is working" shake. A machine calls SetActive(true/false)
    // (or drives Intensity directly); the component jitters a target transform around its
    // original local position while active and eases back to rest when idle.
    //
    // Single responsibility: it only does the shake. Machines decide when they're working.
    public class MachineRumble : MonoBehaviour
    {
        [Tooltip("Transform to shake. Defaults to this object's transform.")]
        [SerializeField] private Transform target;

        [Tooltip("Max positional offset (world units) at full intensity.")]
        [SerializeField] private float amplitude = 0.03f;
        [Tooltip("How jittery the shake is (higher = faster vibration).")]
        [SerializeField] private float frequency = 40f;
        [Tooltip("How fast intensity eases toward its target (per second).")]
        [SerializeField] private float responsiveness = 12f;

        private Vector3 _restLocalPos;
        private float _intensity;     // current, eased 0..1
        private float _targetIntensity; // requested 0..1
        private float _seed;

        private void Awake()
        {
            if (target == null) target = transform;
            _restLocalPos = target.localPosition;
            _seed = Random.value * 100f;
        }

        // Convenience on/off. For proportional shake (e.g. scale with load), set Intensity.
        public void SetActive(bool active) => _targetIntensity = active ? 1f : 0f;

        public float Intensity
        {
            get => _targetIntensity;
            set => _targetIntensity = Mathf.Clamp01(value);
        }

        private void Update()
        {
            _intensity = Mathf.MoveTowards(_intensity, _targetIntensity, responsiveness * Time.deltaTime);

            if (_intensity <= 0.0001f)
            {
                target.localPosition = _restLocalPos;
                return;
            }

            // Perlin noise around the rest position -> smooth but lively jitter.
            float t = Time.time * frequency;
            float ox = (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f;
            float oy = (Mathf.PerlinNoise(_seed + 13.7f, t) - 0.5f) * 2f;

            target.localPosition = _restLocalPos + new Vector3(ox, oy, 0f) * (amplitude * _intensity);
        }

        private void OnDisable()
        {
            if (target != null) target.localPosition = _restLocalPos;
            _intensity = 0f;
            _targetIntensity = 0f;
        }
    }
}