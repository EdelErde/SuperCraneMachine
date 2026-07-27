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

        [Header("Grabbing")]
        [Tooltip("Rigidbody2D on the magnet that caught items attach to.")]
        [SerializeField] private Rigidbody2D magnetRigidbody;
        [SerializeField] private LayerMask grabbableMask = ~0;
        [Tooltip("Higher = items hang more rigidly. Lower = they swing loosely.")]
        [SerializeField] private float jointStiffness = 3f;

        [Header("Tuning")]
        [SerializeField] private MagnetConfig config = new MagnetConfig();

        public State Current { get; private set; } = State.Sweeping;

        private int _sweepDir = 1;
        private float _timer;
        private float _autoTimer;

        private readonly List<Item> _carried = new List<Item>();
        private readonly List<FixedJoint2D> _joints = new List<FixedJoint2D>();
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

        private void Update()
        {
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

        private void Sweep()
        {
            MoveX(_sweepDir);
            if (magnetBody.localPosition.x >= maxX) _sweepDir = -1;
            else if (magnetBody.localPosition.x <= minX) _sweepDir = 1;

            if (MagnetInput.GrabPressed)
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

            var p = magnetBody.localPosition;
            p.y = Mathf.MoveTowards(p.y, bottomY, VerticalSpeed * Time.deltaTime);
            magnetBody.localPosition = p;

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

        // Center and half-extents of the downward square zone hanging from magnetTip.
        private void GetBox(out Vector2 center, out Vector2 halfExtents)
        {
            float side = Mathf.Max(0.01f, MagnetRange);
            float depth = Mathf.Max(0.01f, config.magnetDepth);
            Vector2 tip = magnetTip != null ? (Vector2)magnetTip.position : (Vector2)transform.position;

            // Square is `side` wide, `depth` tall, hanging straight down from the tip.
            center = new Vector2(tip.x, tip.y - depth * 0.5f);
            halfExtents = new Vector2(side * 0.5f, depth * 0.5f);
        }

        private bool BoxOverlapsItem()
        {
            if (magnetTip == null) return false;
            GetBox(out var center, out var half);

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
            GetBox(out var center, out var half);

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
            if (rb == null || magnetRigidbody == null) return;

            var joint = item.gameObject.AddComponent<FixedJoint2D>();
            joint.connectedBody = magnetRigidbody;
            joint.autoConfigureConnectedAnchor = true;
            joint.dampingRatio = 0.6f;
            joint.frequency = jointStiffness;

            _carried.Add(item);
            _joints.Add(joint);
        }

        private void ReleaseCarried()
        {
            foreach (var j in _joints)
                if (j != null) Destroy(j);

            _joints.Clear();
            _carried.Clear();
        }

        private float StatOr(GameStat stat, float fallback) =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(stat) : fallback;

        private void MoveToDrop()
        {
            MoveTowardX(dropX);
            if (Mathf.Abs(magnetBody.localPosition.x - dropX) < 0.05f)
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
            if (Mathf.Abs(magnetBody.localPosition.x - maxX) < 0.05f)
            {
                _sweepDir = -1;
                Current = State.Sweeping;
            }
        }

        private void MoveX(float dir)
        {
            var p = magnetBody.localPosition;
            p.x = Mathf.Clamp(p.x + dir * SweepSpeed * Time.deltaTime, minX, maxX);
            magnetBody.localPosition = p;
        }

        private void MoveTowardX(float targetX)
        {
            var p = magnetBody.localPosition;
            p.x = Mathf.MoveTowards(p.x, targetX, SweepSpeed * Time.deltaTime);
            magnetBody.localPosition = p;
        }

        private void MoveY(float targetY, State next)
        {
            var p = magnetBody.localPosition;
            p.y = Mathf.MoveTowards(p.y, targetY, VerticalSpeed * Time.deltaTime);
            magnetBody.localPosition = p;
            if (Mathf.Abs(p.y - targetY) < 0.02f) Current = next;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (magnetTip == null) return;
            GetBox(out var center, out var half);
            Gizmos.color = Application.isPlaying && Current == State.Grabbing
                ? new Color(0.3f, 1f, 0.4f, 0.9f)
                : new Color(1f, 0.8f, 0.2f, 0.7f);
            Gizmos.DrawWireCube(center, half * 2f);
        }
#endif
    }
}