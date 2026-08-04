using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // Drop-on-machine on/off switch. Binds to any IToggleableMachine (the LeafBlower,
    // the SortingMachine, or anything else that implements it) and flips its power when
    // clicked. Mirrors AutoMagnetToggle's look: on/off text plus an indicator color,
    // using the same active/inactive tint convention as the rest of the UI.
    //
    // Usage: put this on a Button in the machine's canvas/hierarchy. If 'machine' is
    // left empty it grabs the first IToggleableMachine on this object or its parents,
    // so a single prefab works on every machine with no per-instance wiring.
    [RequireComponent(typeof(Button))]
    public class MachineToggleButton : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Machine to switch. Auto-found on this object or a parent if left empty.")]
        [SerializeField] private MonoBehaviour machineSource; // must implement IToggleableMachine

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI label;
        [Tooltip("Optional colored dot/background that reflects on/off.")]
        [SerializeField] private Image indicator;
        [Tooltip("Optional: show the machine's name alongside the state.")]
        [SerializeField] private bool prefixMachineName = false;
        [SerializeField] private string onText = "ON";
        [SerializeField] private string offText = "OFF";

        [Header("On/Off feedback")]
        [Tooltip("Color while the machine is on.")]
        [SerializeField] private Color onColor = new Color(0.4f, 1f, 0.45f);
        [Tooltip("Color while the machine is off.")]
        [SerializeField] private Color offColor = new Color(0.55f, 0.55f, 0.55f);

        private Button _button;
        private IToggleableMachine _machine;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _machine = ResolveMachine();
        }

        private void OnEnable()
        {
            if (_machine == null) _machine = ResolveMachine();

            _button.onClick.AddListener(OnClick);
            if (_machine != null) _machine.OnToggled += OnToggled;

            Refresh();
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
            if (_machine != null) _machine.OnToggled -= OnToggled;
        }

        private IToggleableMachine ResolveMachine()
        {
            if (machineSource is IToggleableMachine fromField) return fromField;
            // Fall back to whatever toggleable machine sits on this object / a parent.
            return GetComponentInParent<IToggleableMachine>();
        }

        private void OnClick()
        {
            if (_machine == null) return;
            _machine.MachineEnabled = !_machine.MachineEnabled;
            // OnToggled will fire Refresh, but call it directly too in case some
            // implementation sets the field without raising the event.
            Refresh();
        }

        private void OnToggled(bool _) => Refresh();

        private void Refresh()
        {
            if (_machine == null) return;

            bool on = _machine.MachineEnabled;

            if (label != null)
            {
                string state = on ? onText : offText;
                label.text = prefixMachineName && !string.IsNullOrEmpty(_machine.ToggleLabel)
                    ? $"{_machine.ToggleLabel}: {state}"
                    : state;
            }

            if (indicator != null) indicator.color = on ? onColor : offColor;
        }
    }
}