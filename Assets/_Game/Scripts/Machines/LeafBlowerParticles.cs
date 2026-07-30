using UnityEngine;

namespace CraneMachine
{
    // Turns a ParticleSystem's emission on while the leaf blower is actively blowing, and
    // aligns the emitter to the blow direction. Keep the ParticleSystem set to Looping = ON,
    // Play On Awake = OFF; this component controls emission enabled/disabled so it ramps in
    // and out cleanly instead of hard cutting.
    [RequireComponent(typeof(LeafBlower))]
    public class LeafBlowerParticles : MonoBehaviour
    {
        [Tooltip("Wind particle system. Should be a child positioned at the nozzle.")]
        [SerializeField] private ParticleSystem wind;
        [Tooltip("Rotate the emitter to face the blow direction each frame.")]
        [SerializeField] private bool alignToBlowDirection = true;

        private LeafBlower _blower;
        private ParticleSystem.EmissionModule _emission;
        private bool _hasEmission;

        private void Awake()
        {
            _blower = GetComponent<LeafBlower>();
            if (wind != null)
            {
                _emission = wind.emission;
                _hasEmission = true;

                var main = wind.main;
                main.playOnAwake = false;
                if (!wind.isPlaying) wind.Play();     // running, but emission gated below
                _emission.enabled = false;
            }
        }

        private void Update()
        {
            if (wind == null || !_hasEmission) return;

            bool blowing = _blower != null && _blower.IsBlowing;
            if (_emission.enabled != blowing)
                _emission.enabled = blowing;

            if (blowing && alignToBlowDirection)
            {
                Vector2 dir = _blower.BlowWorldDirection;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    // Particle systems emit along their local +Z/forward or the shape's axis;
                    // for a 2D cone we point the transform's right (+X) along the blow dir and
                    // let the Shape module's rotation handle the cone. Rotating the whole
                    // emitter object is the simplest robust approach.
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    wind.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }

        private void OnDisable()
        {
            if (wind != null && _hasEmission)
                _emission.enabled = false;
        }
    }
}