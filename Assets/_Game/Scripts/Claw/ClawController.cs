using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class ClawController : MonoBehaviour
    {
        public enum State { Sweeping, Lowering, Grabbing, Raising, MovingToDrop, Dropping, Returning }

        [Header("References")]
        [SerializeField] private Transform clawBody;
        [SerializeField] private ClawProng[] prongs;

        [Header("Grabbing")]
        [Tooltip("Point between the prongs where items are caught.")]
        [SerializeField] private Transform grabPoint;
        [Tooltip("Rigidbody2D on the claw that caught items attach to.")]
        [SerializeField] private Rigidbody2D clawRigidbody;
        [SerializeField] private float grabRadius = 0.5f;
        [SerializeField] private LayerMask grabbableMask = ~0;
        [Tooltip("Higher = items hang more rigidly. Lower = they swing loosely.")]
        [SerializeField] private float jointStiffness = 3f;

        [Header("Tuning")]
        [SerializeField] private ClawConfig config = new ClawConfig();

        public State Current { get; private set; } = State.Sweeping;

        private int _sweepDir = 1;
        private float _timer;
        private float _autoTimer;
        private bool _grabRolled;

        private readonly List<Item> _carried = new List<Item>();
        private readonly List<FixedJoint2D> _joints = new List<FixedJoint2D>();
        private readonly Collider2D[] _grabHits = new Collider2D[16];

        public int CarriedCount => _carried.Count;

        public bool AutoClawUnlocked =>
            ServiceLocator.StatService != null &&
            ServiceLocator.StatService.GameValue(GameStat.AutoClaw) > 0f;

        public bool AutoClawEnabled { get; private set; } = true;

        public bool AutoClawActive => AutoClawUnlocked && AutoClawEnabled;

        public void SetAutoClaw(bool on)
        {
            AutoClawEnabled = on;
            _autoTimer = 0f;
        }

        public void ToggleAutoClaw() => SetAutoClaw(!AutoClawEnabled);

        private float minX => config.minX;
        private float maxX => config.maxX;
        private float topY => config.topY;
        private float bottomY => config.bottomY;
        private float dropX => config.dropX;
        private float grabHold => config.grabHold;
        private float dropHold => config.dropHold;

        // Speeds route through the stat system (upgradeable); config is the base/fallback.
        private float SweepSpeed =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.ClawSweepSpeed) : config.sweepSpeed;
        private float VerticalSpeed =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.ClawVerticalSpeed) : config.verticalSpeed;

        private void Awake() => ServiceLocator.Claw = this;

        private void Start() => OpenProngs();

        private void Update()
        {
            switch (Current)
            {
                case State.Sweeping:     Sweep();        break;
                case State.Lowering:     MoveY(bottomY, State.Grabbing); break;
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
            if (clawBody.localPosition.x >= maxX) _sweepDir = -1;
            else if (clawBody.localPosition.x <= minX) _sweepDir = 1;

            if (ClawInput.GrabPressed)
            {
                _grabRolled = false;
                Current = State.Lowering;
                return;
            }

            TickAutoClaw();
        }

        private void TickAutoClaw()
        {
            if (!AutoClawActive)
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
                _grabRolled = false;
                Current = State.Lowering;
            }
        }

        private float NextAutoDelay()
        {
            float avg = ServiceLocator.StatService != null
                ? ServiceLocator.StatService.GameValue(GameStat.AutoClawInterval)
                : 8f;
            avg = Mathf.Max(0.5f, avg);
            return Random.Range(avg * 0.5f, avg * 1.5f);
        }

        private void Grab()
        {
            CloseProngs();
            _timer += Time.deltaTime;

            if (!_grabRolled && _timer >= grabHold * 0.5f)
            {
                _grabRolled = true;
                TryCatchItems();
            }

            if (_timer >= grabHold)
            {
                _timer = 0f;
                Current = State.Raising;
            }
        }

        private void TryCatchItems()
        {
            if (grabPoint == null) return;

            int capacity = Mathf.Max(1, Mathf.RoundToInt(StatOr(GameStat.ClawGrabCapacity, 1f)));
            float chance = Mathf.Clamp01(StatOr(GameStat.ClawGrabStrength, 0.35f));

            int found = Physics2D.OverlapCircle(
                grabPoint.position, grabRadius,
                new ContactFilter2D { useLayerMask = true, layerMask = grabbableMask, useTriggers = false },
                _grabHits);

            var candidates = new List<Item>();
            for (int i = 0; i < found; i++)
            {
                var item = _grabHits[i].GetComponentInParent<Item>();
                if (item != null && !candidates.Contains(item)) candidates.Add(item);
            }

            candidates.Sort((a, b) =>
                ((Vector2)a.transform.position - (Vector2)grabPoint.position).sqrMagnitude
                .CompareTo(((Vector2)b.transform.position - (Vector2)grabPoint.position).sqrMagnitude));

            foreach (var item in candidates)
            {
                if (_carried.Count >= capacity) break;
                if (Random.value > chance) continue;
                Attach(item);
            }
        }

        private void Attach(Item item)
        {
            var rb = item.GetComponent<Rigidbody2D>();
            if (rb == null || clawRigidbody == null) return;

            var joint = item.gameObject.AddComponent<FixedJoint2D>();
            joint.connectedBody = clawRigidbody;
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
            if (Mathf.Abs(clawBody.localPosition.x - dropX) < 0.05f)
                Current = State.Dropping;
        }

        private void Drop()
        {
            OpenProngs();
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
            if (Mathf.Abs(clawBody.localPosition.x - maxX) < 0.05f)
            {
                _sweepDir = -1;
                Current = State.Sweeping;
            }
        }

        private void MoveX(float dir)
        {
            var p = clawBody.localPosition;
            p.x = Mathf.Clamp(p.x + dir * SweepSpeed * Time.deltaTime, minX, maxX);
            clawBody.localPosition = p;
        }

        private void MoveTowardX(float targetX)
        {
            var p = clawBody.localPosition;
            p.x = Mathf.MoveTowards(p.x, targetX, SweepSpeed * Time.deltaTime);
            clawBody.localPosition = p;
        }

        private void MoveY(float targetY, State next)
        {
            var p = clawBody.localPosition;
            p.y = Mathf.MoveTowards(p.y, targetY, VerticalSpeed * Time.deltaTime);
            clawBody.localPosition = p;
            if (Mathf.Abs(p.y - targetY) < 0.02f) Current = next;
        }

        private void OpenProngs()  { foreach (var pr in prongs) pr.Open(); }
        private void CloseProngs() { foreach (var pr in prongs) pr.Close(); }
    }
}