using UnityEngine;
using UnityEngine.Audio;

namespace CraneMachine
{
    // Central volume control. Put on one always-active object, assign the AudioMixer,
    // and expose "MasterVol", "SfxVol", "MusicVol" params on the mixer (right-click a
    // group's Volume in the mixer -> Expose to script, then rename in the Exposed list).
    //
    // Set volumes with 0..1 sliders; they map to decibels internally.
    public class AudioMixerController : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;

        [Header("Exposed parameter names (must match the mixer)")]
        [SerializeField] private string masterParam = "MasterVol";
        [SerializeField] private string sfxParam = "SfxVol";
        [SerializeField] private string musicParam = "MusicVol";

        [Header("Startup levels (0..1)")]
        [SerializeField, Range(0f, 1f)] private float master = 1f;
        [SerializeField, Range(0f, 1f)] private float sfx = 1f;
        [SerializeField, Range(0f, 1f)] private float music = 1f;

        private const string PrefMaster = "vol_master";
        private const string PrefSfx = "vol_sfx";
        private const string PrefMusic = "vol_music";

        private void Awake()
        {
            ServiceLocator.Audio = this;

            master = PlayerPrefs.GetFloat(PrefMaster, master);
            sfx = PlayerPrefs.GetFloat(PrefSfx, sfx);
            music = PlayerPrefs.GetFloat(PrefMusic, music);
        }

        private void Start()
        {
            SetMaster(master);
            SetSfx(sfx);
            SetMusic(music);
        }
        public void SetMaster(float v) => Apply(masterParam, PrefMaster, ref master, v);
        public void SetSfx(float v)    => Apply(sfxParam, PrefSfx, ref sfx, v);
        public void SetMusic(float v)  => Apply(musicParam, PrefMusic, ref music, v);

        public float Master => master;
        public float Sfx => sfx;
        public float Music => music;

        private void Apply(string param, string prefKey, ref float field, float value01)
        {
            field = Mathf.Clamp01(value01);
            if (mixer != null && !string.IsNullOrEmpty(param))
                mixer.SetFloat(param, LinearToDb(field));
            PlayerPrefs.SetFloat(prefKey, field);
        }

        private static float LinearToDb(float v) =>
            v <= 0.0001f ? -80f : Mathf.Log10(v) * 20f;
    }
}