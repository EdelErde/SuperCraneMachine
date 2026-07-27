using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    public class SpawnCountdownDisplay : MonoBehaviour
    {
        [Header("Fill")]
        [Tooltip("Image with Image Type = Filled, Fill Method = Radial 360.")]
        [SerializeField] private Image fill;
        [Tooltip("Fill empties toward the spawn instead of filling up.")]
        [SerializeField] private bool invert;
        [Tooltip("Smooths the fill so it doesn't jump when a cycle resets.")]
        [SerializeField] private float fillSmoothing = 14f;

        [Header("Colour")]
        [SerializeField] private Color startColor = new Color(0.5f, 0.75f, 1f);
        [SerializeField] private Color readyColor = new Color(0.45f, 1f, 0.5f);
        [SerializeField] private Color pausedColor = new Color(0.6f, 0.6f, 0.6f);
        [Tooltip("Progress at which the colour starts shifting to 'ready'.")]
        [Range(0f, 1f)]
        [SerializeField] private float readyAt = 0.75f;

        [Header("Optional label")]
        [SerializeField] private TextMeshProUGUI secondsLabel;
        [SerializeField] private string secondsFormat = "{0:0.0}s";
        [SerializeField] private string pausedText = "FULL";

        [Header("Pop")]
        [SerializeField] private bool pop = true;
        [SerializeField] private float popScale = 1.25f;
        [SerializeField] private float popDecay = 5f;

        [Header("Paused pulse")]
        [SerializeField] private bool pulseWhenPaused = true;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseAmount = 0.05f;

        private RectTransform _rect;
        private Vector3 _baseScale;
        private float _shown;
        private float _pop;
        private ItemSpawner _spawner;

        private void Awake()
        {
            _rect = (fill != null ? fill.rectTransform : GetComponent<RectTransform>());
            if (_rect != null) _baseScale = _rect.localScale;
        }

        private void Start() => TryBind();

        private void OnDestroy()
        {
            if (_spawner != null) _spawner.OnSpawned -= OnSpawned;
        }

        private void TryBind()
        {
            if (_spawner != null) return;
            _spawner = ServiceLocator.ItemSpawner;
            if (_spawner != null) _spawner.OnSpawned += OnSpawned;
        }

        private void OnSpawned()
        {
            if (pop) _pop = 1f;
        }

        private void Update()
        {
            TryBind();
            if (_spawner == null) return;

            bool paused = _spawner.Paused;
            float target = paused ? 1f : _spawner.SpawnProgress;

            // Snap backwards on cycle reset, smooth going forwards.
            if (target < _shown - 0.2f) _shown = target;
            else _shown = Mathf.Lerp(_shown, target, 1f - Mathf.Exp(-fillSmoothing * Time.deltaTime));

            if (fill != null)
            {
                fill.fillAmount = invert ? 1f - _shown : _shown;
                fill.color = paused
                    ? pausedColor
                    : Color.Lerp(startColor, readyColor, Mathf.InverseLerp(readyAt, 1f, _shown));
            }

            if (secondsLabel != null)
                secondsLabel.text = paused
                    ? pausedText
                    : string.Format(secondsFormat, _spawner.TimeToNextSpawn);

            UpdateScale(paused);
        }

        private void UpdateScale(bool paused)
        {
            if (_rect == null) return;

            float scale = 1f;

            if (pop)
            {
                if (_pop > 0f)
                    _pop = Mathf.Max(0f, _pop - popDecay * Time.deltaTime);
                float t = _pop * _pop;
                scale *= Mathf.Lerp(1f, popScale, t);
            }

            if (pulseWhenPaused && paused)
                scale *= 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

            _rect.localScale = _baseScale * scale;
        }
    }
}