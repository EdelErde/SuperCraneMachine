using TMPro;
using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(TextMeshPro))]
    public class MoneyFlyout : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1.1f;
        [SerializeField] private float riseDistance = 1.4f;
        [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve alphaCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f));
        [SerializeField] private float popScale = 1.25f;
        [SerializeField] private Color moneyColor = new Color(0.35f, 0.95f, 0.4f);

        private TextMeshPro _tmp;
        private Vector3 _start;
        private float _t;
        private float _baseScale = 1f;

        private void Awake() => _tmp = GetComponent<TextMeshPro>();

        public void Play(int amount, Vector3 worldPos, float baseScale = 1f)
        {
            _tmp = _tmp != null ? _tmp : GetComponent<TextMeshPro>();
            _start = worldPos;
            _baseScale = baseScale;
            transform.position = worldPos;
            _tmp.text = "$" + amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            _tmp.color = moneyColor;
            _t = 0f;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / lifetime);

            transform.position = _start + Vector3.up * (riseCurve.Evaluate(k) * riseDistance);

            var c = _tmp.color;
            c.a = alphaCurve.Evaluate(k);
            _tmp.color = c;

            float pop = k < 0.2f ? Mathf.Lerp(popScale, 1f, k / 0.2f) : 1f;
            transform.localScale = Vector3.one * _baseScale * pop;

            if (k >= 1f) Destroy(gameObject);
        }
    }
}