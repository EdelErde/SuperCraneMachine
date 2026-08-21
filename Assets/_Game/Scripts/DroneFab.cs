using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // The box hanging from the ceiling. Tin cans go in the right side (the `entry`
    // trigger); after the production time the fab rumbles and spits a Drone out the left
    // side (`exit`). Mirrors FuelFilter's buffer/timer/produce structure and its
    // separate-collider entry-relay pattern, so a can dropped into the mouth is consumed
    // exactly like the fuel filter consumes eggs.
    //
    // Holds the DroneRouteConfig every drone it spawns reads from, so all a fab's drones
    // share one routing table the player edits in the Drone Setup window.
    public class DroneFab : MonoBehaviour, IWorldInteractable
    {
        public Transform Transform => transform;

        [Header("Config")]
        [Tooltip("Item type consumed to build a drone. Default: Tin Can.")]
        [SerializeReference] private ItemType accepts = new TinCan();

        [Tooltip("Which screen this fab's drones work on. Drones search every loose item on " +
                 "this screen's area (see ScreenArea) — not a radius. Set this to the screen " +
                 "the fab lives on. Drones can be reassigned to another screen at runtime " +
                 "via Drone.AssignScreen.")]
        [SerializeField] private ScreenId screen = ScreenId.Screen1;

        [Tooltip("Drone prefab spawned out the exit. Must have a Drone component.")]
        [SerializeField] private Drone dronePrefab;

        [Tooltip("Per-fab routing table shared by every drone this fab makes. " +
                 "Edited at runtime via the Drone Setup window.")]
        [SerializeField] private DroneRouteConfig config = new DroneRouteConfig();

        [Header("Points (place in scene)")]
        [Tooltip("Trigger tin cans are dropped into (right side). Can be a collider on THIS " +
                 "object or a separate one anywhere (a relay is auto-added there).")]
        [SerializeField] private Collider2D entry;
        [Tooltip("Where finished drones are spawned (left side). Defaults to this transform.")]
        [SerializeField] private Transform exit;
        [Tooltip("Point near the fab where idle drones loiter. Defaults to the exit.")]
        [SerializeField] private Transform idlePoint;

        [Header("Feel")]
        [Tooltip("Max drones this fab keeps alive at once. Extra cans queue until a slot frees.")]
        [SerializeField] private int maxLiveDrones = 3;

        [Header("Feedback (optional)")]
        [Tooltip("Rumble that plays while a drone is being built.")]
        [SerializeField] private MachineRumble rumble;

        public event System.Action OnIntake;    // a can was accepted
        public event System.Action OnProduce;   // a drone popped out

        public DroneRouteConfig Config => config;

        // Which screen this fab's drones are assigned to work on.
        public ScreenId Screen => screen;

        // Upgradeable: seconds to build one drone (lower = faster). Floored so upgrades
        // can't drive it to zero. Matches FuelFilterProcessTime's "lower is better" style.
        private float ProductionTime =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0.1f, ServiceLocator.StatService.GameValue(GameStat.DroneProductionTime))
                : 4f;

        // Upgradeable: how many deliveries a fresh drone can make before it dies.
        private int Charges =>
            ServiceLocator.StatService != null
                ? Mathf.Max(1, Mathf.RoundToInt(ServiceLocator.StatService.GameValue(GameStat.DroneCharges)))
                : 5;

        private class Pending { public float readyAt; }
        private readonly List<Pending> _queue = new List<Pending>();
        private readonly HashSet<Item> _known = new HashSet<Item>();
        private readonly List<Drone> _live = new List<Drone>();

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            var own = GetComponent<Collider2D>();
            if (entry == null) entry = own;
            if (exit == null) exit = transform;
            if (idlePoint == null) idlePoint = exit;
            WireEntry();
        }

        // Same relay trick as FuelFilter: Unity only delivers OnTriggerEnter2D to a
        // component on the collider's own GameObject, so if the entry mouth is a separate
        // object we drop a relay there that forwards intakes back to us.
        private void WireEntry()
        {
            if (entry == null)
            {
                Debug.LogWarning("[DroneFab] No entry collider set and none on this object — " +
                                 "the fab can't receive tin cans.", this);
                return;
            }
            entry.isTrigger = true;

            if (entry.gameObject != gameObject)
            {
                var relay = entry.GetComponent<DroneFabEntryRelay>();
                if (relay == null) relay = entry.gameObject.AddComponent<DroneFabEntryRelay>();
                relay.Bind(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (entry != null && entry.gameObject != gameObject) return; // relay handles it
            TryIntake(other);
        }

        // Public so DroneFabEntryRelay can forward from a separate entry collider.
        public void TryIntake(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item == null || _known.Contains(item)) return;
            if (item.type == null || accepts == null || item.type.GetType() != accepts.GetType()) return;

            _known.Add(item);
            item.OnDragEnd();

            // Consume the can immediately (it's "inside" the fab now) and queue a build.
            Destroy(item.gameObject);
            _queue.Add(new Pending { readyAt = Time.time + ProductionTime });
            OnIntake?.Invoke();
        }

        private void Update()
        {
            _known.RemoveWhere(it => it == null);
            _live.RemoveAll(d => d == null);

            bool building = false;

            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                if (Time.time < _queue[i].readyAt) { building = true; continue; }

                // Wait for a free drone slot before producing.
                if (_live.Count >= Mathf.Max(1, maxLiveDrones)) { building = true; continue; }

                ProduceDrone();
                _queue.RemoveAt(i);
            }

            if (rumble != null) rumble.SetActive(building);
        }

        private void ProduceDrone()
        {
            if (dronePrefab == null)
            {
                Debug.LogWarning("[DroneFab] No drone prefab assigned — nothing to spawn.", this);
                return;
            }

            var point = exit != null ? exit : transform;
            var drone = Instantiate(dronePrefab, point.position, Quaternion.identity);
            Vector2 idle = idlePoint != null ? (Vector2)idlePoint.position : (Vector2)point.position;
            drone.Init(this, Charges, idle, screen);

            _live.Add(drone);
            OnProduce?.Invoke();
        }

        // Called by a drone when it dies so the fab can free its slot.
        public void NotifyDroneDied(Drone drone)
        {
            _live.Remove(drone);
        }

        private void OnDrawGizmosSelected()
        {
            if (exit != null)
            {
                Gizmos.color = new Color(0.3f, 0.7f, 1f);
                Gizmos.DrawWireSphere(exit.position, 0.15f);
            }
            if (idlePoint != null)
            {
                Gizmos.color = new Color(0.3f, 1f, 0.6f);
                Gizmos.DrawWireSphere(idlePoint.position, 0.2f);
            }
        }
    }
}