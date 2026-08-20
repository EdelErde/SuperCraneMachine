using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // The Fuel Filter. Player drags the configured input item type (default: Egg) into
    // it; after processTime seconds (read from GameStat.FuelFilterProcessTime, so it's
    // upgradeable — lower = faster) the input item is consumed and a physical Fuel item
    // is spawned out the exit. The player then drags that Fuel item into a FuelFunnel
    // to actually add it to the shared fuel pool.
    //
    // Mirrors SortingMachine's buffer/timer/eject structure.
    //
    // The `entry` trigger can live on this object OR on a separate collider elsewhere
    // (see FuelFilterEntryRelay), so a collider on this GameObject is no longer required.
    public class FuelFilter : MonoBehaviour, IWorldInteractable
    {
        public Transform Transform => transform;

        [Header("Config")]
        [Tooltip("Item type this filter accepts as input. Others are ignored (left for other machines).")]
        [SerializeReference] private ItemType accepts = new Egg();

        [Tooltip("Source of the Fuel item prefab to spawn. Falls back to the spawner's database.")]
        [SerializeField] private ItemDatabase database;

        [Header("Points (place in scene)")]
        [Tooltip("Trigger the player drops input items into. Can be a collider on THIS object " +
                 "or a separate collider anywhere in the scene. If null, uses this object's " +
                 "collider (if any).")]
        [SerializeField] private Collider2D entry;
        [Tooltip("Where produced Fuel items are spawned/ejected. Defaults to this transform.")]
        [SerializeField] private Transform exit;

        [Header("Feel")]
        [Tooltip("Speed Fuel items are ejected out of the exit.")]
        [SerializeField] private float ejectSpeed = 2f;
        [Tooltip("Extra impulse force pushed in the exit direction on release, on top of the eject speed. 0 = velocity only.")]
        [SerializeField] private float ejectForce = 3f;
        [Tooltip("Direction Fuel items are launched from the exit, in the filter's LOCAL space. " +
                 "Leave at (0,0) to use the exit transform's own 'right' axis instead.")]
        [SerializeField] private Vector2 ejectDir = new Vector2(0f, -1f);

        [Header("Liquid droplets (optional)")]
        [Tooltip("Emit fuel liquid droplets from the exit each time this filter produces fuel. " +
                 "Requires a LiquidFieldSystem in the scene (Tools > Liquid Field > Create Liquid Field Setup).")]
        [SerializeField] private bool emitLiquidDroplets = true;
        [Tooltip("How many droplets to spray out per produced fuel item.")]
        [SerializeField] private int dropletsPerProduce = 6;
        [Tooltip("Random positional spread (world units) around the exit that droplets spawn within.")]
        [SerializeField] private float dropletSpread = 0.15f;
        [Tooltip("Random extra velocity (world units/sec) added to each droplet on top of the eject velocity.")]
        [SerializeField] private float dropletVelocityJitter = 0.5f;

        [Header("Feedback (optional)")]
        [Tooltip("Rumble that plays while the filter is processing an item.")]
        [SerializeField] private MachineRumble rumble;

        // SFX lives in the dedicated SFX/ components (see FuelFilterSfx), which listen
        // to these events rather than the filter owning sound config itself.
        public event System.Action OnIntake;
        public event System.Action OnProduce;

        private class Pending
        {
            public Item item;
            public float readyAt;
        }

        private readonly List<Pending> _buffer = new List<Pending>();
        private readonly HashSet<Item> _known = new HashSet<Item>();

        private int Capacity =>
            ServiceLocator.StatService != null
                ? Mathf.Max(1, Mathf.RoundToInt(ServiceLocator.StatService.GameValue(GameStat.FuelFilterCapacity)))
                : 4;

        // Upgradeable — lower value = faster processing. Buying the upgrade lowers
        // this stat (see FuelFilterSpeedUpgrade), unlike most stats where higher = better.
        private float ProcessTime =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0.05f, ServiceLocator.StatService.GameValue(GameStat.FuelFilterProcessTime))
                : 1.5f;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            var ownCollider = GetComponent<Collider2D>();

            // Default the entry to this object's own collider if one exists and none was assigned.
            if (entry == null) entry = ownCollider;
            if (exit == null) exit = transform;

            WireEntry();
        }

        // Make the `entry` collider actually deliver intakes. Unity only sends
        // OnTriggerEnter2D to a component on the SAME GameObject as the colliding
        // collider, so if `entry` is a separate object we attach a relay there that
        // forwards back to us. If `entry` is our own collider, this object's own
        // OnTriggerEnter2D handles it and no relay is needed.
        private void WireEntry()
        {
            if (entry == null)
            {
                Debug.LogWarning("[FuelFilter] No entry collider set and none on this object — " +
                                 "the filter has no way to receive items.", this);
                return;
            }

            // Ensure the entry collider is a trigger (that's how items are detected).
            entry.isTrigger = true;

            // If the entry lives on another GameObject, put a relay there to forward events.
            if (entry.gameObject != gameObject)
            {
                var relay = entry.GetComponent<FuelFilterEntryRelay>();
                if (relay == null) relay = entry.gameObject.AddComponent<FuelFilterEntryRelay>();
                relay.Bind(this);
            }
        }

        private ItemDatabase ResolveDatabase()
        {
            if (database != null) return database;
            return ServiceLocator.ItemSpawner != null ? ServiceLocator.ItemSpawner.Database : null;
        }

        // Fires only when the entry collider is on THIS GameObject. When entry is a
        // separate collider, FuelFilterEntryRelay calls TryIntake directly instead.
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Only handle it here if our own collider is the entry; otherwise the relay does.
            if (entry != null && entry.gameObject != gameObject) return;
            TryIntake(other);
        }

        // Attempt to take in a colliding item. Public so FuelFilterEntryRelay can forward
        // trigger events from a separate entry collider. Runs the accept/capacity checks
        // and buffers the item for processing.
        public void TryIntake(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item == null || _known.Contains(item)) return;
            if (item.type == null || accepts == null || item.type.GetType() != accepts.GetType()) return;
            if (_buffer.Count >= Capacity) return;

            _known.Add(item);
            item.OnDragEnd();

            var rb = item.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;

            SetItemVisible(item, false);

            _buffer.Add(new Pending { item = item, readyAt = Time.time + ProcessTime });
            OnIntake?.Invoke();
        }

        private void Update()
        {
            PurgeDestroyed();

            for (int i = _buffer.Count - 1; i >= 0; i--)
            {
                var p = _buffer[i];
                if (p.item == null) { _buffer.RemoveAt(i); continue; }
                if (Time.time < p.readyAt) continue;

                Produce(p.item);
                _buffer.RemoveAt(i);
            }

            if (rumble != null) rumble.SetActive(_buffer.Count > 0);
        }

        private void PurgeDestroyed()
        {
            for (int i = _buffer.Count - 1; i >= 0; i--)
                if (_buffer[i] == null || _buffer[i].item == null)
                    _buffer.RemoveAt(i);

            _known.RemoveWhere(it => it == null);
        }

        // Consume the buffered input item and spawn a Fuel item in its place.
        private void Produce(Item input)
        {
            _known.Remove(input);
            Destroy(input.gameObject);

            var db = ResolveDatabase();
            var prefab = db != null ? db.Find<Fuel>() : null;
            if (prefab == null)
            {
                Debug.LogWarning("[FuelFilter] No Fuel item prefab found in the ItemDatabase — " +
                                 "add one (SpawnWeight 0) so the filter has something to spawn.", this);
                return;
            }

            var point = exit != null ? exit : transform;
            var go = Instantiate(prefab, point.position, Quaternion.identity);

            var fuelItem = go.GetComponent<Item>();
            SetItemVisible(fuelItem, true);

            Vector2 ejectVelocity = Vector2.zero;
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                Vector2 dir = EjectDirection(point);
                ejectVelocity = dir * ejectSpeed;
                rb.linearVelocity = ejectVelocity;
                if (ejectForce > 0f)
                    rb.AddForce(dir * ejectForce, ForceMode2D.Impulse);
            }

            EmitLiquidDroplets(point, ejectVelocity);

            OnProduce?.Invoke();
        }

        // Spray a burst of fuel liquid droplets from the exit so produced fuel reads as
        // a gooey liquid mass (see LiquidFieldSystem). No-op if the system isn't present.
        private void EmitLiquidDroplets(Transform point, Vector2 baseVelocity)
        {
            if (!emitLiquidDroplets || dropletsPerProduce <= 0) return;
            if (LiquidFieldSystem.Instance == null) return;

            Vector2 origin = point != null ? (Vector2)point.position : (Vector2)transform.position;

            for (int i = 0; i < dropletsPerProduce; i++)
            {
                Vector2 pos = origin + Random.insideUnitCircle * dropletSpread;
                Vector2 vel = baseVelocity + Random.insideUnitCircle * dropletVelocityJitter;
                LiquidFieldSystem.Spawn(LiquidType.Fuel, pos, vel);
            }
        }

        private Vector2 EjectDirection(Transform point)
        {
            if (ejectDir.sqrMagnitude < 0.0001f)
                return point != null ? (Vector2)point.right : Vector2.right;

            return (Vector2)transform.TransformDirection(ejectDir.normalized);
        }

        private static void SetItemVisible(Item item, bool visible)
        {
            if (item == null) return;
            var renderers = item.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = visible;
        }

        private void OnDrawGizmosSelected()
        {
            if (exit == null) return;

            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            Vector3 origin = exit.position;
            Gizmos.DrawWireSphere(origin, 0.12f);

            Vector3 dir = (Vector3)EjectDirection(exit);
            float len = 0.6f;
            Vector3 tip = origin + dir * len;
            Gizmos.DrawLine(origin, tip);
        }
    }
}