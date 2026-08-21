using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // One routing rule: "any loose item of <type> should be flown to the destination
    // named <destinationId>." Mirrors SortingConfig's SortRule shape so it feels the
    // same to author. destinationId is a DroneDestination.Id (its GameObject name).
    [Serializable]
    public class DroneRoute
    {
        [SerializeReference] public ItemType type = new Fuel();
        public string destinationId = "";
    }

    // Per-fab routing config the player edits via the Drone Setup window (drag an item's
    // icon onto a destination column, exactly like the Sorter Dialog). Each item TYPE
    // has at most one destination — assigning a new one replaces the old (that's the
    // "each item can only have 1 destination point" rule). Unassigned types are ignored
    // by drones (left for the hand / other machines).
    [Serializable]
    public class DroneRouteConfig
    {
        [SerializeField] private List<DroneRoute> routes = new List<DroneRoute>();

        public IReadOnlyList<DroneRoute> Routes => routes;

        public event Action OnChanged;

        public DroneRoute GetRoute(Type itemType)
        {
            if (itemType == null) return null;
            for (int i = 0; i < routes.Count; i++)
                if (routes[i].type != null && routes[i].type.GetType() == itemType)
                    return routes[i];
            return null;
        }

        // The destination id an item type is routed to, or "" if unassigned.
        public string DestinationIdFor(Type itemType)
        {
            var r = GetRoute(itemType);
            return r != null ? r.destinationId : "";
        }

        // Resolve to a live DroneDestination (null if unassigned or the object is gone).
        public DroneDestination DestinationFor(Type itemType)
        {
            string id = DestinationIdFor(itemType);
            return string.IsNullOrEmpty(id) ? null : DroneDestination.Find(id);
        }

        public bool IsRouted(Type itemType) => DestinationFor(itemType) != null;

        // Assign a type to a destination. Passing null/empty id clears the assignment
        // (one destination per type — this overwrites any existing one).
        public void SetDestination(ItemType type, string destinationId)
        {
            if (type == null) return;

            var route = GetRoute(type.GetType());

            if (string.IsNullOrEmpty(destinationId))
            {
                if (route != null) routes.Remove(route);
                OnChanged?.Invoke();
                return;
            }

            if (route == null)
            {
                route = new DroneRoute { type = type, destinationId = destinationId };
                routes.Add(route);
            }
            else
            {
                route.destinationId = destinationId;
            }
            OnChanged?.Invoke();
        }

        public void Clear(ItemType type) => SetDestination(type, null);
    }
}