using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    [RequireComponent(typeof(Button))]
    public class BuyableNotifier : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Graphic tintGraphic;
        [SerializeField] private float bounceScale = 1.15f;
        [SerializeField] private float bounceSpeed = 6f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.55f);
        [SerializeField, Range(0f, 1f)] private float tintStrength = 0.6f;
        [SerializeField] private float checkInterval = 0.25f;

        private Vector3 _baseScale;
        private Color _baseColor;
        private bool _active;
        private bool _dismissed;
        private float _nextCheck;
        private readonly HashSet<System.Type> _known = new HashSet<System.Type>();

        private void Awake()
        {
            if (target == null) target = transform as RectTransform;
            if (tintGraphic == null) tintGraphic = GetComponent<Graphic>();
            _baseScale = target != null ? target.localScale : Vector3.one;
            if (tintGraphic != null) _baseColor = tintGraphic.color;
            GetComponent<Button>().onClick.AddListener(Dismiss);
        }

        private void Dismiss()
        {
            _dismissed = true;
            _active = false;
            Rest();
            Seed();
        }

        private void Seed()
        {
            _known.Clear();
            var svc = ServiceLocator.UpgradeService;
            if (svc == null) return;
            foreach (var u in svc.AllUpgrades())
                if (u != null && svc.CanAfford(u)) _known.Add(u.GetType());
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextCheck)
            {
                _nextCheck = Time.unscaledTime + checkInterval;
                Reevaluate();
            }

            if (!_active) return;
            float t = (Mathf.Sin(Time.unscaledTime * bounceSpeed) + 1f) * 0.5f;
            if (target != null) target.localScale = _baseScale * Mathf.Lerp(1f, bounceScale, t);
            if (tintGraphic != null)
                tintGraphic.color = Color.Lerp(_baseColor, highlightColor, tintStrength * t);
        }

        private void Reevaluate()
        {
            var svc = ServiceLocator.UpgradeService;
            if (svc == null) return;

            bool anyAffordable = false;
            foreach (var u in svc.AllUpgrades())
            {
                if (u == null) continue;
                if (svc.CanAfford(u))
                {
                    anyAffordable = true;
                    if (_known.Add(u.GetType()) && !_dismissed) _active = true;
                }
                else _known.Remove(u.GetType());
            }

            if (!anyAffordable) { _dismissed = false; _active = false; Rest(); }
        }

        private void Rest()
        {
            if (target != null) target.localScale = _baseScale;
            if (tintGraphic != null) tintGraphic.color = _baseColor;
        }
    }
}