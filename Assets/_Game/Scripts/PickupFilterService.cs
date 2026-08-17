using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Tracks which item types the player has blocked from the mouse drag-pickup
    // mechanic (via the Pickup Filter panel, opened with RMB). Blocked types can
    // still be moved by machines (conveyors, sorters, drones, etc.) — this only
    // gates WorldInteractionController's manual pickup.
    // Registers itself into ServiceLocator.PickupFilter, mirroring FuelConsumerRegistry.
    public class PickupFilterService : MonoBehaviour
    {
        private readonly HashSet<Type> _blocked = new HashSet<Type>();

        public event Action OnChanged;

        private void Awake() => ServiceLocator.PickupFilter = this;

        private void OnDestroy()
        {
            if (ServiceLocator.PickupFilter == this)
                ServiceLocator.PickupFilter = null;
        }

        public bool IsBlocked(Type itemType) => itemType != null && _blocked.Contains(itemType);

        public bool IsBlocked(ItemType type) => type != null && IsBlocked(type.GetType());

        public void SetBlocked(ItemType type, bool blocked)
        {
            if (type == null) return;
            SetBlocked(type.GetType(), blocked);
        }

        public void SetBlocked(Type itemType, bool blocked)
        {
            if (itemType == null) return;

            bool changed = blocked ? _blocked.Add(itemType) : _blocked.Remove(itemType);
            if (changed) OnChanged?.Invoke();
        }

        public void Toggle(ItemType type)
        {
            if (type == null) return;
            SetBlocked(type, !IsBlocked(type));
        }
    }
}