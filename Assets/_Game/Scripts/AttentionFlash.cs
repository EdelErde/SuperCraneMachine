using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // Drop-on-any-button "look at me" flash. Reproduces the BuyableNotifier effect
    // (a bounce-scale plus a color tint pulse) but decoupled from the upgrade system:
    // it simply starts flashing whenever the object is enabled and stops the moment the
    // player interacts with the button. Put it on anything that has a Button and you
    // want to draw attention to when it first appears.
    [RequireComponent(typeof(Button))]
    public class AttentionFlash : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("Transform that gets scaled. Defaults to this object's RectTransform.")]
        [SerializeField] private RectTransform target;
        [Tooltip("Graphic that gets tinted. Defaults to this object's Graphic.")]
        [SerializeField] private Graphic tintGraphic;

        [Header("Bounce")]
        [SerializeField] private float bounceScale = 1.15f;
        [SerializeField] private float bounceSpeed = 6f;

        [Header("Tint")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.55f);
        [SerializeField, Range(0f, 1f)] private float tintStrength = 0.6f;

        [Header("Behaviour")]
        [Tooltip("Flash again every time the object is re-enabled, even after a previous dismiss.")]
        [SerializeField] private bool restartOnEnable = true;

        private Button _button;
        private Vector3 _baseScale;
        private Color _baseColor;
        private bool _active;
        private bool _captured;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (target == null) target = transform as RectTransform;
            if (tintGraphic == null) tintGraphic = GetComponent<Graphic>();

            // Capture the resting look once, before we ever start animating it.
            _baseScale = target != null ? target.localScale : Vector3.one;
            if (tintGraphic != null) _baseColor = tintGraphic.color;
            _captured = true;

            _button.onClick.AddListener(Dismiss);
        }

        private void OnEnable()
        {
            // Awake runs before the first OnEnable, so _baseScale/_baseColor are valid.
            if (restartOnEnable || !_active) _active = true;
        }

        private void OnDisable()
        {
            // Leave the button looking normal while hidden.
            _active = false;
            Rest();
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(Dismiss);
        }

        // Called when the player clicks the button. Also callable manually if some other
        // interaction (hover, etc.) should stop the flash.
        public void Dismiss()
        {
            if (!_active) return;
            _active = false;
            Rest();
        }

        // Start flashing again on demand (e.g. new content appeared).
        public void Flash() => _active = true;

        private void Update()
        {
            if (!_active) return;

            float t = (Mathf.Sin(Time.unscaledTime * bounceSpeed) + 1f) * 0.5f;

            if (target != null)
                target.localScale = _baseScale * Mathf.Lerp(1f, bounceScale, t);

            if (tintGraphic != null)
                tintGraphic.color = Color.Lerp(_baseColor, highlightColor, tintStrength * t);
        }

        private void Rest()
        {
            if (!_captured) return; // never animated yet; nothing to restore
            if (target != null) target.localScale = _baseScale;
            if (tintGraphic != null) tintGraphic.color = _baseColor;
        }
    }
}