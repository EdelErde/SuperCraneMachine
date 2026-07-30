using UnityEngine;
using UnityEngine.Audio;

namespace CraneMachine
{
    // Core SFX player. Add to any GameObject, assign clips, call Play().
    // Play() is public + parameterless, so it also works in a Button's OnClick.
    // Assign 'output' (an SFX mixer group) so central volume control affects it.
    [RequireComponent(typeof(AudioSource))]
    public class SfxSource : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private float pitchJitter = 0.05f;
        [Tooltip("Ignore repeat plays within this many seconds (0 = no limit).")]
        [SerializeField] private float minInterval = 0f;
        [Tooltip("Route through this mixer group so central volume applies. Optional.")]
        [SerializeField] private AudioMixerGroup output;

        private AudioSource _src;
        private float _next;

        private void Awake()
        {
            _src = GetComponent<AudioSource>();
            _src.playOnAwake = false;
            if (output != null) _src.outputAudioMixerGroup = output;
        }

        public void Play()
        {
            if (clips == null || clips.Length == 0) return;
            if (minInterval > 0f && Time.time < _next) return;
            _next = Time.time + minInterval;

            var clip = clips[Random.Range(0, clips.Length)];
            _src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            _src.PlayOneShot(clip, volume);
        }
    }
}