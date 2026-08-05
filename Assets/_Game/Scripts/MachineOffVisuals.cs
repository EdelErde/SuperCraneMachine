using UnityEngine;

namespace CraneMachine
{
    public class MachineOffVisuals : MonoBehaviour
    {
        [Tooltip("Machine to watch. Auto-found on this object or a parent if left empty.")]
        [SerializeField] private MonoBehaviour machineSource;
        [Tooltip("Colliders turned off while the machine is off.")]
        [SerializeField] private Collider2D[] colliders;

        [Tooltip("Sprites dimmed while the machine is off.")]
        [SerializeField] private SpriteRenderer[] sprites;

        [Tooltip("Opacity (0-1) sprites fade to while the machine is off.")]
        [SerializeField, Range(0f, 1f)] private float offOpacity = 0.4f;

        private IToggleableMachine _machine;

        private void OnEnable()
        {
            _machine = machineSource as IToggleableMachine ?? GetComponentInParent<IToggleableMachine>();
            if (_machine != null)
            {
                _machine.OnToggled += Apply;
                Apply(_machine.MachineEnabled);
            }
        }

        private void OnDisable()
        {
            if (_machine != null) _machine.OnToggled -= Apply;
        }

        private void Apply(bool on)
        {
            if (colliders != null)
                foreach (var c in colliders)
                    if (c != null) c.enabled = on;

            if (sprites != null)
                foreach (var sr in sprites)
                    if (sr != null)
                    {
                        var col = sr.color;
                        col.a = on ? 1f : offOpacity;
                        sr.color = col;
                    }
        }
    }
}