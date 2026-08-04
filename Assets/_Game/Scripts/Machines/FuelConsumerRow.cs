using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // One row in the fuel-consumer list: a machine's name plus its live fuel draw
    // (units per second) right now. Bound to a single IFuelConsumer and polled each
    // frame, since CurrentFuelDraw changes continuously as the machine runs.
    //
    // Mirrors ProductionCard's structure. The on/off visual feedback follows the same
    // active/inactive tint convention used elsewhere in the UI (e.g. UpgradeButton's
    // affordable vs. unaffordable colors): full-strength color while burning fuel,
    // dimmed while idle.
    public class FuelConsumerRow : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI label;      // machine name, e.g. "Leaf Blower"
        [SerializeField] private TextMeshProUGUI rateLabel;  // live draw, e.g. "0.5/s"
        [Tooltip("Format for the draw line. {0} = fuel units per second.")]
        [SerializeField] private string rateFormat = "{0:0.0}/s";

        [Header("On/Off feedback")]
        [Tooltip("Optional dot/icon that lights up while the machine is drawing fuel.")]
        [SerializeField] private Image statusIcon;
        [Tooltip("Color while the machine is on (drawing fuel).")]
        [SerializeField] private Color activeColor = Color.white;
        [Tooltip("Color while the machine is off (idle, no draw).")]
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);
        [Tooltip("Draw above this (units/sec) counts as 'on'.")]
        [SerializeField] private float onThreshold = 0.0001f;

        private IFuelConsumer _consumer;
        private bool _wasActive;
        private bool _hasState;

        public IFuelConsumer Consumer => _consumer;

        // Called by FuelConsumerView right after Instantiate.
        public void Bind(IFuelConsumer consumer)
        {
            _consumer = consumer;
            _hasState = false;

            if (label != null && consumer != null)
                label.text = consumer.FuelLabel;

            Refresh(force: true);
        }

        private void Update() => Refresh(force: false);

        private void Refresh(bool force)
        {
            if (_consumer == null) return;

            float draw = _consumer.CurrentFuelDraw;
            bool active = draw > onThreshold;

            if (rateLabel != null)
                rateLabel.text = string.Format(rateFormat, Mathf.Max(0f, draw));

            // Only repaint on state change (or first bind) so we're not touching
            // colors every frame.
            if (force || !_hasState || active != _wasActive)
            {
                Color c = active ? activeColor : inactiveColor;
                if (label != null) label.color = c;
                if (rateLabel != null) rateLabel.color = c;
                if (statusIcon != null) statusIcon.color = c;

                _wasActive = active;
                _hasState = true;
            }
        }
    }
}