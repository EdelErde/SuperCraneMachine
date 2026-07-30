using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Anything that burns fuel and wants to be shown in the production view implements this.
    public interface IFuelConsumer
    {
        string FuelLabel { get; }      // display name, e.g. "Leaf Blower"
        float CurrentFuelDraw { get; } // fuel units per second right now (0 when idle)
    }

    // Central list of active fuel consumers, so the production view can show per-machine
    // consumption and the net fuel rate without each machine knowing about the UI.
    // Registers itself into ServiceLocator.FuelConsumers.
    public class FuelConsumerRegistry : MonoBehaviour
    {
        private readonly List<IFuelConsumer> _consumers = new List<IFuelConsumer>();

        public IReadOnlyList<IFuelConsumer> Consumers => _consumers;

        private void Awake() => ServiceLocator.FuelConsumers = this;

        private void OnDestroy()
        {
            if (ServiceLocator.FuelConsumers == this)
                ServiceLocator.FuelConsumers = null;
        }

        public void Register(IFuelConsumer consumer)
        {
            if (consumer != null && !_consumers.Contains(consumer))
                _consumers.Add(consumer);
        }

        public void Unregister(IFuelConsumer consumer)
        {
            _consumers.Remove(consumer);
        }

        // Total fuel/sec being drawn across all machines right now.
        public float TotalDraw()
        {
            float sum = 0f;
            for (int i = 0; i < _consumers.Count; i++)
                if (_consumers[i] != null) sum += _consumers[i].CurrentFuelDraw;
            return sum;
        }
    }
}