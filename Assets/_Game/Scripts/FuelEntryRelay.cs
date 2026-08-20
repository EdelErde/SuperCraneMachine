using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// Forwards 2D trigger-enter events from the collider on THIS GameObject to a
    /// FuelFilter that lives elsewhere. Unity only delivers OnTriggerEnter2D to a
    /// component on the same GameObject as the colliding collider, so a FuelFilter whose
    /// `entry` collider is a separate child/object never hears about intakes on its own.
    /// This relay closes that gap: put it (automatically, via FuelFilter) on the entry
    /// collider's object and it calls back into the filter.
    ///
    /// Added and wired automatically by FuelFilter when an `entry` collider is assigned,
    /// so you normally never touch this component directly.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FuelFilterEntryRelay : MonoBehaviour
    {
        private FuelFilter _filter;

        public void Bind(FuelFilter filter)
        {
            _filter = filter;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_filter != null) _filter.TryIntake(other);
        }
    }
}