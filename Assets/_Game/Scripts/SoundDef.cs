using UnityEngine;
using UnityEngine.Audio;

namespace CraneMachine
{
    // One sound, fully defined in-editor as an entry in SfxManager.sounds — not an
    // asset, not spread across other components. Add an entry, name it, pick what
    // triggers it (a plain dropdown over every moment SfxManager knows about — see
    // SfxTrigger), configure clips/volume/screens/concurrency, done. SfxManager itself
    // owns firing the trigger (subscribing to the relevant machine/service event or
    // polling state) — nothing needs to be wired up anywhere else in the scene.
    [System.Serializable]
    public class SoundDef
    {
        [Tooltip("Just a label for this entry in the list — not referenced by anything.")]
        [SerializeField] private string id;

        [Tooltip("Which moment fires this sound.")]
        [SerializeField] private SfxTrigger trigger;

        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private float pitchJitter = 0.05f;
        [Tooltip("Ignore repeat plays within this many seconds (0 = no limit).")]
        [SerializeField] private float minInterval = 0f;
        [Tooltip("Route through this mixer group so central volume applies. Optional.")]
        [SerializeField] private AudioMixerGroup output;

        [Header("Screens")]
        [Tooltip("Which screen(s) this sound is allowed to play on. Pick 'Everything' for a " +
                 "sound that should play regardless of which screen is currently active " +
                 "(e.g. UI clicks, cash gain). If the currently active screen (see " +
                 "ScreenCameraRef.Current) isn't included, this sound is silently skipped.")]
        [SerializeField] private ScreenMask screens = ScreenMask.Everything;

        [Header("Concurrency")]
        [Tooltip("If unchecked, this sound draws from the shared global voice pool/limit. " +
                 "If checked, it draws from its own separate pool/limit instead (pick a category below).")]
        [SerializeField] private bool useSeparateLimit;
        [SerializeField] private SfxCategory category;
        [Tooltip("Max simultaneous voices for this category. Only used when 'Use Separate Limit' is checked.")]
        [SerializeField] private int maxConcurrent = 4;

        public string Id => id;
        public SfxTrigger Trigger => trigger;
        public AudioClip[] Clips => clips;
        public float Volume => volume;
        public float PitchJitter => pitchJitter;
        public float MinInterval => minInterval;
        public AudioMixerGroup Output => output;
        public bool UseSeparateLimit => useSeparateLimit;
        public SfxCategory Category => category;
        public int MaxConcurrent => maxConcurrent;

        public bool AllowedOn(ScreenId screen) => screens.HasFlag(ScreenMaskUtil.From(screen));

        [System.NonSerialized] private float _nextAllowedTime;

        public bool OnCooldown(float time) => minInterval > 0f && time < _nextAllowedTime;
        public void StartCooldown(float time) => _nextAllowedTime = time + minInterval;

        public bool HasClips => clips != null && clips.Length > 0;

        public AudioClip PickClip() =>
            HasClips ? clips[Random.Range(0, clips.Length)] : null;
    }
}