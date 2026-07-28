using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // Scrap magnet controller.
    public class MagnetController : MonoBehaviour
    {
        public enum State { Sweeping, Lowering, Grabbing, Raising, MovingToDrop, Dropping, Returning }

        [Header("References")]
        [SerializeField] private Transform magnetBody;
        [Tooltip("Bottom face of the magnet. The square pickup zone hangs straight down from here.")]
        [SerializeField] private Transform magnetTip;
        [Tooltip("The magnet's visual (sprite). Its X scale is stretched to match MagnetRange. " +
                 "Leave empty to skip visual scaling.")]
        [SerializeField] private Transform magnetVisual;

        [Header("Grabbing")]
        [Tooltip("Rigidbody2D on the magnet that caught items attach to.")]
        [SerializeField] private Rigidbody2D magnetRigidbody;
        [SerializeField] private LayerMask grabbableMask = ~0;

        [Header("Tuning")]
        [SerializeField] private MagnetConfig config = new MagnetConfig();

        public State Current { get; private set; } = State.Sweeping;

        private int _sweepDir = 1;
        private float _timer;
        private float _autoTimer;

        private readonly List<Item> _carried = new List<Item>();
        private readonly List<Rigidbody2D> _carriedBodies = new List<Rigidbody2D>();
        private readonly Collider2D[] _grabHits = new Collider2D[32];

        public int CarriedCount => _carried.Count;

        public bool AutoMagnetUnlocked =>
            ServiceLocator.StatService != null &&
            ServiceLocator.StatService.GameValue(GameStat.AutoMagnet) > 0f;

        public bool AutoMagnetEnabled { get; private set; } = true;

        public bool AutoMagnetActive => AutoMagnetUnlocked && AutoMagnetEnabled;

        public void SetAutoMagnet(bool on)
        {
            AutoMagnetEnabled = on;
            _autoTimer = 0f;
        }

        public void ToggleAutoMagnet() => SetAutoMagnet(!AutoMagnetEnabled);

        private float minX => config.minX;
        private float maxX => config.maxX;
        private float topY => config.topY;
        private float bottomY => config.bottomY;
        private float dropX => config.dropX;
        private float grabHold => config.grabHold;
        private float dropHold => config.dropHold;

        // Speeds route through the stat system (upgradeable); config is the base/fallback.
        private float SweepSpeed =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.MagnetSweepSpeed) : config.sweepSpeed;
        private float VerticalSpeed =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.MagnetVerticalSpeed) : config.verticalSpeed;

        // Side length (width) of the square pickup zone (world units), upgradeable.
        private float MagnetRange =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.MagnetRange) : config.magnetRange;

        private void Awake() => ServiceLocator.Magnet = this;

        // Visual-only work stays on the render frame.
        private void Update() => SyncVisualWidth();

        // Movement runs in physics so held items are pulled along.
        private void FixedUpdate()
        {
            // Continuous magnetic attraction on all held items, every physics step.
            ApplyMagnetism();

            switch (Current)
            {
                case State.Sweeping:     Sweep();        break;
                case State.Lowering:     Lower();        break;
                case State.Grabbing:     Grab();         break;
                case State.Raising:      MoveY(topY, State.MovingToDrop); break;
                case State.MovingToDrop: MoveToDrop();   break;
                case State.Dropping:     Drop();         break;
                case State.Returning:    Return();       break;
            }
        }

        // Current magnet position. Body and rigidbody are the same root object,
        // so rigidbody position == transform position == local position.
        private Vector2 Pos =>
            magnetRigidbody != null ? magnetRigidbody.position : (Vector2)magnetBody.localPosition;

        // Move the magnet through physics so joints and carried items follow.
        private void SetPos(Vector2 p)
        {
            if (magnetRigidbody != null) magnetRigidbody.MovePosition(p);
            else magnetBody.localPosition = p;
        }

        private void Sweep()
        {
            MoveX(_sweepDir);
            if (Pos.x >= maxX) _sweepDir = -1;
            else if (Pos.x <= minX) _sweepDir = 1;

            if (MagnetInput.ConsumeGrab())
            {
                Current = State.Lowering;
                return;
            }

            TickAutoMagnet();
        }

        private void TickAutoMagnet()
        {
            if (!AutoMagnetActive)
            {
                _autoTimer = 0f;
                return;
            }

            if (_autoTimer <= 0f)
            {
                _autoTimer = NextAutoDelay();
                return;
            }

            _autoTimer -= Time.deltaTime;
            if (_autoTimer <= 0f)
            {
                _autoTimer = NextAutoDelay();
                Current = State.Lowering;
            }
        }

        private float NextAutoDelay()
        {
            float avg = ServiceLocator.StatService != null
                ? ServiceLocator.StatService.GameValue(GameStat.AutoMagnetInterval)
                : 8f;
            avg = Mathf.Max(0.5f, avg);
            return Random.Range(avg * 0.5f, avg * 1.5f);
        }

        // Descend until the square zone touches an item, or until the floor (bottomY).
        private void Lower()
        {
            if (BoxOverlapsItem())
            {
                _timer = 0f;
                Current = State.Grabbing;
                return;
            }

            var p = Pos;
            p.y = Mathf.MoveTowards(p.y, bottomY, VerticalSpeed * Time.fixedDeltaTime);
            SetPos(p);

            if (Mathf.Abs(p.y - bottomY) < 0.02f)
            {
                // Reached the floor without touching anything — grab whatever's here (maybe nothing).
                _timer = 0f;
                Current = State.Grabbing;
            }
        }

        private void Grab()
        {
            // Engage the magnet: attach everything in the square, closest-first, up to capacity.
            if (_carried.Count == 0)
                MagnetizeItems();

            _timer += Time.deltaTime;
            if (_timer >= grabHold)
            {
                _timer = 0f;
                Current = State.Raising;
            }
        }

        // Stretch the magnet sprite's width so it visually matches the pickup range.
        private void SyncVisualWidth()
        {
            if (magnetVisual == null) return;

            float refWidth = Mathf.Max(0.01f, config.spriteReferenceWidth);
            float scaleX = Mathf.Max(0.01f, MagnetRange) / refWidth;

            var s = magnetVisual.localScale;
            // Preserve sign (in case the sprite is flipped) and leave Y/Z alone.
            s.x = Mathf.Sign(s.x == 0f ? 1f : s.x) * scaleX;
            magnetVisual.localScale = s;
        }

        private float DetectionDepth => Mathf.Max(0.01f, config.detectionDepth);
        // Upgradeable via the MagnetDepth stat; config is the fallback.
        private float PickupDepth =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0.01f, ServiceLocator.StatService.GameValue(GameStat.MagnetDepth))
                : Mathf.Max(0.01f, config.pickupDepth);

        // Center and half-extents of a downward box (MagnetRange wide, `depth` tall) hanging from magnetTip.
        private void GetBox(float depth, out Vector2 center, out Vector2 halfExtents)
        {
            float side = Mathf.Max(0.01f, MagnetRange);
            depth = Mathf.Max(0.01f, depth);
            Vector2 tip = magnetTip != null ? (Vector2)magnetTip.position : (Vector2)transform.position;

            // Box top edge sits at the tip; it extends `depth` straight down.
            center = new Vector2(tip.x, tip.y - depth * 0.5f);
            halfExtents = new Vector2(side * 0.5f, depth * 0.5f);
        }

        // Thin probe at the tip: is the magnet face touching an item?
        private bool BoxOverlapsItem()
        {
            if (magnetTip == null) return false;
            GetBox(DetectionDepth, out var center, out var half);

            int found = Physics2D.OverlapBox(
                center, half * 2f, 0f,
                new ContactFilter2D { useLayerMask = true, layerMask = grabbableMask, useTriggers = false },
                _grabHits);

            for (int i = 0; i < found; i++)
                if (_grabHits[i].GetComponentInParent<Item>() != null)
                    return true;

            return false;
        }

        private void MagnetizeItems()
        {
            if (magnetTip == null) return;
            // Bigger pickup zone: grabs everything around the contact point, not just what triggered detection.
            GetBox(PickupDepth, out var center, out var half);

            int capacity = Mathf.Max(1, Mathf.RoundToInt(StatOr(GameStat.MagnetGrabCapacity, 1f)));

            int found = Physics2D.OverlapBox(
                center, half * 2f, 0f,
                new ContactFilter2D { useLayerMask = true, layerMask = grabbableMask, useTriggers = false },
                _grabHits);

            var candidates = new List<Item>();
            for (int i = 0; i < found; i++)
            {
                var item = _grabHits[i].GetComponentInParent<Item>();
                if (item != null && !candidates.Contains(item)) candidates.Add(item);
            }

            // Closest to the magnet tip first, so the cap keeps the nearest scrap.
            Vector2 tip = magnetTip.position;
            candidates.Sort((a, b) =>
                ((Vector2)a.transform.position - tip).sqrMagnitude
                .CompareTo(((Vector2)b.transform.position - tip).sqrMagnitude));

            foreach (var item in candidates)
            {
                if (_carried.Count >= capacity) break;
                Attach(item);
            }
        }

        private void Attach(Item item)
        {
            var rb = item.GetComponent<Rigidbody2D>();
            if (rb == null || _carriedBodies.Contains(rb)) return;
            _carried.Add(item);
            _carriedBodies.Add(rb);
        }

        // Called every physics step while items are held: pull each toward the tip with an
        // inverse-distance force, damp its velocity so it settles, and release it if the
        // player is dragging it hard enough to overpower the magnet.
        private void ApplyMagnetism()
        {
            if (_carried.Count == 0 || magnetTip == null) return;
            Vector2 tip = magnetTip.position;

            float baseForce = config.attractForce;
            float soft = Mathf.Max(0.01f, config.attractSoftening);
            float maxF = config.attractMaxForce;
            float hand = StatOr(GameStat.HandStrength, 0.3f);

            for (int i = _carried.Count - 1; i >= 0; i--)
            {
                var item = _carried[i];
                var rb = _carriedBodies[i];
                if (item == null || rb == null) { RemoveAt(i); continue; }

                // Pull-off: if the player is dragging this item, hand strength competes with pull.
                if (item.IsDragging)
                {
                    // Effective magnet grip at this item's distance.
                    Vector2 d0 = tip - rb.position;
                    float dist0 = d0.magnitude;
                    float grip = Mathf.Min(maxF, baseForce / (dist0 * dist0 + soft * soft));
                    // Hand pulls with a force proportional to HandStrength; scale to comparable units.
                    float pull = hand * 100f;
                    if (pull > grip * config.breakFreeFactor)
                    {
                        RemoveAt(i);   // player wins the tug of war — item leaves the magnet
                        continue;
                    }
                }

                Vector2 d = tip - rb.position;
                float dist = d.magnitude;
                if (dist > 0.0001f)
                {
                    // Inverse-distance: stronger as the item nears the tip, clamped near zero.
                    float mag = Mathf.Min(maxF, baseForce / (dist * dist + soft * soft));
                    rb.AddForce(d.normalized * mag);
                }
                // Damp so items settle and clump at the tip instead of orbiting/oscillating.
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, config.holdDamping * Time.fixedDeltaTime);
            }
        }

        private void RemoveAt(int i)
        {
            _carried.RemoveAt(i);
            _carriedBodies.RemoveAt(i);
        }

        private void ReleaseCarried()
        {
            _carried.Clear();
            _carriedBodies.Clear();
        }

        private float StatOr(GameStat stat, float fallback) =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(stat) : fallback;

        private void MoveToDrop()
        {
            MoveTowardX(dropX);
            if (Mathf.Abs(Pos.x - dropX) < 0.05f)
                Current = State.Dropping;
        }

        private void Drop()
        {
            // Disengage the magnet.
            if (_carried.Count > 0) ReleaseCarried();
            _timer += Time.deltaTime;
            if (_timer >= dropHold)
            {
                _timer = 0f;
                Current = State.Returning;
            }
        }

        private void Return()
        {
            MoveTowardX(maxX);
            if (Mathf.Abs(Pos.x - maxX) < 0.05f)
            {
                _sweepDir = -1;
                Current = State.Sweeping;
            }
        }

        private void MoveX(float dir)
        {
            var p = Pos;
            p.x = Mathf.Clamp(p.x + dir * SweepSpeed * Time.fixedDeltaTime, minX, maxX);
            SetPos(p);
        }

        private void MoveTowardX(float targetX)
        {
            var p = Pos;
            p.x = Mathf.MoveTowards(p.x, targetX, SweepSpeed * Time.fixedDeltaTime);
            SetPos(p);
        }

        private void MoveY(float targetY, State next)
        {
            var p = Pos;
            p.y = Mathf.MoveTowards(p.y, targetY, VerticalSpeed * Time.fixedDeltaTime);
            SetPos(p);
            if (Mathf.Abs(p.y - targetY) < 0.02f) Current = next;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (magnetTip == null) return;

            GetBox(PickupDepth, out var pCenter, out var pHalf);
            Gizmos.color = Application.isPlaying && Current == State.Grabbing
                ? new Color(0.3f, 1f, 0.4f, 0.9f)
                : new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(pCenter, pHalf * 2f);

            GetBox(DetectionDepth, out var dCenter, out var dHalf);
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            Gizmos.DrawWireCube(dCenter, dHalf * 2f);
        }
#endif
    }
}