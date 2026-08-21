using UnityEngine;

namespace CraneMachine
{
    // Sits on a separate `entry` collider and forwards OnTriggerEnter2D back to the
    // DroneFab, because Unity only delivers the callback to a component on the SAME
    // GameObject as the colliding collider. Auto-added by DroneFab.WireEntry() — you
    // don't add this by hand. Identical pattern to FuelFilterEntryRelay.
    public class DroneFabEntryRelay : MonoBehaviour
    {
        private DroneFab _fab;

        public void Bind(DroneFab fab) => _fab = fab;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_fab != null) _fab.TryIntake(other);
        }
    }
}