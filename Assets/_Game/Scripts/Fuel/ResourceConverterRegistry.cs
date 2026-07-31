using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Central list of active resource converters, so the ProductionView can generate one card
    // per converter at runtime (like UpgradeView generates buttons). Add a new resource later
    // = drop a new ResourceConverter in the scene; it registers here and the view shows it.
    [DefaultExecutionOrder(-100)]
    public class ResourceConverterRegistry : MonoBehaviour
    {
        private readonly List<ResourceConverter> _converters = new List<ResourceConverter>();

        public IReadOnlyList<ResourceConverter> Converters => _converters;

        // Raised when a converter is added or removed, so views can rebuild their cards.
        public event Action OnChanged;

        private void Awake() => ServiceLocator.ResourceConverters = this;

        private void OnDestroy()
        {
            if (ServiceLocator.ResourceConverters == this)
                ServiceLocator.ResourceConverters = null;
        }

        public void Register(ResourceConverter converter)
        {
            if (converter == null || _converters.Contains(converter)) return;
            _converters.Add(converter);
            OnChanged?.Invoke();
        }

        public void Unregister(ResourceConverter converter)
        {
            if (_converters.Remove(converter))
                OnChanged?.Invoke();
        }
    }
}