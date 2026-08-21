using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    // A little helicopter that carries loose items to their assigned destination.
    //
    // CARRY MODEL — the important part:
    // The drone carries an item the EXACT same way the player's hand does. It never
    // reparents or freezes the item; it just calls the item's IDraggable drag API
    // (OnDragBegin / OnDrag / OnDragEnd) and moves the drag target to a point just under
    // itself. The item stays a normal live Rigidbody2D the whole time. This buys three
    // things for free:
    //   1. The item swings/settles under the drone with the same juice as hand-carry.
    //   2. The player can "steal" it: WorldInteractionController grabs any IDraggable in
    //      range, which flips the item's drag target away from us. We detect that the
    //      item is no longer following us (it's being dragged elsewhere OR its regrab
    //      cooldown fired because it slipped) and let go.
    //   3. TUG-OF-WAR: while carrying, the item pulls back on the drone (equal-and-
    //      opposite spring). So when the hand yanks the item, the item drags the drone
    //      toward the cursor for a moment before the drone snaps free — the "juicy"
    //      option you picked.
    //
    // MOVEMENT FEEL: helicopter-ish. A bouncy spring toward the current goal + gentle
    // idle bob, velocity damping as a "brake path" so it eases into stops instead of
    // snapping. Amplitude is kept modest because the ceiling space is tight.
    //
    // PATHFINDING: lightweight steering, not A*. The drone seeks its goal directly and
    // casts a couple of short whisker rays ahead; if a wall is close it adds a sideways
    // avoidance nudge. For a mostly-open ceiling area with the odd wall this is plenty,
    // costs nothing, and never needs a baked grid. (If drones ever get boxed into a maze,
    // that's the point to add a real graph — flagged, not built.)
    [RequireComponent(typeof(Rigidbody2D))]
    public class Drone : MonoBehaviour
    {
        public enum State { Idle, Seeking, Carrying, Returning, Dying }

        [Header("Routing (set by the fab)")]
        [Tooltip("The route config this drone reads assignments from. The fab assigns this " +
                 "when it spawns the drone; you normally don't set it by hand.")]
        [SerializeField] private DroneFab fab;

        [Header("Movement feel")]
        [Tooltip("Spring stiffness pulling the drone toward its goal. Higher = snappier.")]
        [SerializeField] private float moveAccel = 22f;
        [Tooltip("Velocity damping (the 'brake path'). Higher = eases into stops harder.")]
        [SerializeField] private float damping = 4.5f;
        [Tooltip("Hard cap on drone speed (world units/sec). Kept modest for the tight space.")]
        [SerializeField] private float maxSpeed = 4f;

        [Header("Jiggle / idle bob")]
        [Tooltip("Idle bob amplitude (world units). Small — the space is cramped.")]
        [SerializeField] private float bobAmplitude = 0.06f;
        [Tooltip("Idle bob speed.")]
        [SerializeField] private float bobFrequency = 3.2f;
        [Tooltip("Constant tiny positional jitter added to the goal so it never sits dead still.")]
        [SerializeField] private float jitter = 0.03f;

        [Header("Work")]
        [Tooltip("How close (world units) the drone must get to a loose item before it grips it.")]
        [SerializeField] private float grabRadius = 0.35f;
        [Tooltip("How far the drone will look for assigned loose items to pick up.")]
        [SerializeField] private float searchRadius = 8f;
        [Tooltip("Layers loose items live on (leave Everything if unsure).")]
        [SerializeField] private LayerMask itemLayers = ~0;
        [Tooltip("Vertical offset of the carried item below the drone (world units).")]
        [SerializeField] private float carryDrop = 0.35f;

        [Header("Tug-of-war (stealing)")]
        [Tooltip("How hard the carried item pulls back on the drone. 0 = drone unaffected " +
                 "by the item; higher = drone gets yanked more when you steal.")]
        [SerializeField] private float tugFactor = 6f;
        [Tooltip("Seconds the drone keeps trying to hold an item that's being pulled away " +
                 "before it gives up (the 'brief tug' window).")]
        [SerializeField] private float tugGiveUpTime = 0.35f;
        [Tooltip("If the carried item drifts further than this from the drone's carry point, " +
                 "treat it as stolen and let go.")]
        [SerializeField] private float snapDistance = 1.1f;

        [Header("Feedback (optional)")]
        [SerializeField] private MachineRumble bodyRumble;
        [Tooltip("Fades the sprite out over the death animation when charges run out.")]
        [SerializeField] private SpriteRenderer[] fadeRenderers;

        // ---- Upgradeable stats (read live from StatService, like every other machine) ----
        // Speed multiplier on empty travel. DroneSpeed default 1.
        private float SpeedMul =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0.1f, ServiceLocator.StatService.GameValue(GameStat.DroneSpeed))
                : 1f;
        // Speed multiplier while carrying (usually a bit lower — feels weighed down).
        private float CarrySpeedMul =>
            ServiceLocator.StatService != null
                ? Mathf.Max(0.1f, ServiceLocator.StatService.GameValue(GameStat.DroneCarrySpeed))
                : 0.8f;

        private Rigidbody2D _rb;
        private State _state = State.Idle;
        private int _chargesLeft;
        private Item _carried;
        private DroneDestination _target;    // where the carried item is going
        private Item _seekTarget;            // the loose item we're flying toward to grab
        private float _bobPhase;
        private float _tugTimer;
        private float _dieTimer;
        private Vector2 _homeIdle;           // a point near the fab to loiter at when idle

        private readonly Collider2D[] _hits = new Collider2D[32];

        public State CurrentState => _state;
        public int ChargesLeft => _chargesLeft;
        public bool IsBusy => _state == State.Seeking || state == StateAliasCarrying();

        // tiny helper to keep the expression above readable without a second enum compare typo
        private State StateAliasCarrying() => State.Carrying;
        private State state => _state;

        // Called by the fab right after Instantiate.
        public void Init(DroneFab owner, int charges, Vector2 idlePoint)
        {
            fab = owner;
            _chargesLeft = Mathf.Max(1, charges);
            _homeIdle = idlePoint;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;          // helicopters hover; no gravity
            _bobPhase = Random.value * 100f;
            if (_homeIdle.sqrMagnitude < 0.0001f) _homeIdle = _rb.position;
        }

        private void FixedUpdate()
        {
            switch (_state)
            {
                case State.Idle:      TickIdle();      break;
                case State.Seeking:   TickSeeking();   break;
                case State.Carrying:  TickCarrying();  break;
                case State.Returning: TickReturning(); break;
                case State.Dying:     TickDying();     break;
            }

            if (bodyRumble != null)
                bodyRumble.Intensity = _rb.linearVelocity.magnitude / Mathf.Max(0.01f, maxSpeed);
        }

        // ---------------------------------------------------------------- IDLE
        private void TickIdle()
        {
            // Look for an assigned loose item to go grab.
            var item = FindAssignedLooseItem();
            if (item != null)
            {
                _seekTarget = item;
                _state = State.Seeking;
                return;
            }

            // Nothing to do: bob gently near home.
            MoveToward(_homeIdle, SpeedMul);
        }

        // ---------------------------------------------------------------- SEEKING
        private void TickSeeking()
        {
            // Item vanished or got picked up / blocked while we flew over.
            if (_seekTarget == null || !CanTake(_seekTarget))
            {
                _seekTarget = null;
                _state = State.Idle;
                return;
            }

            Vector2 itemPos = _seekTarget.Transform.position;
            MoveToward(itemPos, SpeedMul);

            if (Vector2.Distance(_rb.position, itemPos) <= grabRadius)
                Grip(_seekTarget);
        }

        // ---------------------------------------------------------------- CARRYING
        private void TickCarrying()
        {
            // Lost the item (destroyed, consumed, or stolen and already gone).
            if (_carried == null) { OnLostItem(); return; }

            Vector2 carryPoint = _rb.position + Vector2.up * -carryDrop; // just below the drone

            // Detect a steal: the player has grabbed the item, so its drag target is no
            // longer the point WE set. We can't read their target directly, but we CAN
            // see the item drifting away from our carry point (they're pulling it), and
            // we can see it's still being dragged. If it drifts past snapDistance, or the
            // tug window elapses while it's clearly fighting us, we let go.
            float drift = Vector2.Distance(_carried.Transform.position, carryPoint);

            bool beingFought = drift > grabRadius * 1.5f;
            if (beingFought) _tugTimer += Time.fixedDeltaTime;
            else _tugTimer = 0f;

            if (drift > snapDistance || _tugTimer >= tugGiveUpTime)
            {
                // TUG-OF-WAR payoff: before releasing, the item yanks the drone toward it
                // (equal-and-opposite), so the drone visibly lurches after the stolen item.
                Vector2 pull = ((Vector2)_carried.Transform.position - _rb.position);
                _rb.AddForce(pull * tugFactor, ForceMode2D.Impulse);
                ReleaseCarried(stolen: true);
                return;
            }

            // Keep telling the item to follow our carry point (this IS the hand's drag).
            _carried.OnDrag(carryPoint);

            // The item pulls back on us continuously while carried — this is what makes
            // the whole rig feel connected and lets a heavy item weigh the drone down.
            Vector2 tug = ((Vector2)_carried.Transform.position - carryPoint) * tugFactor;
            _rb.AddForce(tug);

            // Fly toward the destination's drop point.
            if (_target == null) { ReleaseCarried(stolen: false); return; }

            Vector2 drop = _target.DropPoint;
            MoveToward(drop, CarrySpeedMul);

            if (Vector2.Distance(_carried.Transform.position, drop) <= _target.ReleaseRadius)
                Deliver();
        }

        // ---------------------------------------------------------------- RETURNING
        private void TickReturning()
        {
            MoveToward(_homeIdle, SpeedMul);

            if (Vector2.Distance(_rb.position, _homeIdle) <= 0.4f)
            {
                if (_chargesLeft <= 0) { _state = State.Dying; _dieTimer = 0f; }
                else _state = State.Idle;
            }
        }

        // ---------------------------------------------------------------- DYING
        private void TickDying()
        {
            _dieTimer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(_dieTimer / 0.8f);

            // Sink and fade.
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.9f, -0.6f);
            if (fadeRenderers != null)
                for (int i = 0; i < fadeRenderers.Length; i++)
                {
                    if (fadeRenderers[i] == null) continue;
                    var c = fadeRenderers[i].color;
                    c.a = 1f - t;
                    fadeRenderers[i].color = c;
                }

            if (t >= 1f)
            {
                if (fab != null) fab.NotifyDroneDied(this);
                Destroy(gameObject);
            }
        }

        // ---------------------------------------------------------------- MOVEMENT
        // Bouncy spring toward a goal with a brake path (velocity damping) + idle bob +
        // whisker obstacle avoidance. speedMul scales the top speed for empty vs carrying.
        private void MoveToward(Vector2 goal, float speedMul)
        {
            _bobPhase += Time.fixedDeltaTime * bobFrequency;
            float bob = Mathf.Sin(_bobPhase) * bobAmplitude;
            Vector2 jitterVec = new Vector2(
                (Mathf.PerlinNoise(_bobPhase, 0f) - 0.5f),
                (Mathf.PerlinNoise(0f, _bobPhase) - 0.5f)) * (jitter * 2f);

            Vector2 wobbleGoal = goal + new Vector2(0f, bob) + jitterVec;

            Vector2 toGoal = wobbleGoal - _rb.position;
            Vector2 desiredAccel = toGoal * moveAccel;

            // Whisker avoidance: cast ahead along velocity; if a wall is near, push sideways.
            desiredAccel += AvoidWalls();

            // Spring + brake: accelerate toward goal, damp current velocity.
            _rb.AddForce(desiredAccel - _rb.linearVelocity * damping);

            // Clamp top speed (scaled by upgrade).
            float cap = maxSpeed * speedMul;
            if (_rb.linearVelocity.magnitude > cap)
                _rb.linearVelocity = _rb.linearVelocity.normalized * cap;
        }

        // Two short whisker rays angled off the current heading. If either hits a wall
        // close by, return a sideways+backward nudge away from it. Cheap, no grid.
        private Vector2 AvoidWalls()
        {
            Vector2 vel = _rb.linearVelocity;
            if (vel.sqrMagnitude < 0.01f) return Vector2.zero;

            Vector2 dir = vel.normalized;
            float look = 0.8f;
            Vector2 result = Vector2.zero;

            // Only avoid things NOT on the item layer (walls/geometry). We reuse the item
            // mask by inverting it: anything the drone shouldn't fly through is "solid".
            int solidMask = ~itemLayers.value;

            Vector2[] whiskers =
            {
                Rotate(dir, 20f),
                Rotate(dir, -20f),
            };

            for (int i = 0; i < whiskers.Length; i++)
            {
                RaycastHit2D hit = Physics2D.Raycast(_rb.position, whiskers[i], look, solidMask);
                if (hit.collider != null)
                {
                    float closeness = 1f - Mathf.Clamp01(hit.distance / look);
                    // Push along the surface normal, away from the wall.
                    result += hit.normal * (moveAccel * closeness);
                }
            }
            return result;
        }

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }

        // ---------------------------------------------------------------- GRIP / RELEASE
        private void Grip(Item item)
        {
            if (item == null) return;

            _carried = item;
            _target = fab != null ? fab.Config.DestinationFor(item.type.GetType()) : null;
            _seekTarget = null;
            _tugTimer = 0f;

            // Carry it the hand's way: begin a drag at the item's current position.
            item.OnDragBegin(item.Transform.position);
            _state = State.Carrying;
        }

        // Successful drop at the destination.
        private void Deliver()
        {
            if (_carried != null) _carried.OnDragEnd();
            _carried = null;
            _target = null;

            _chargesLeft--;
            _state = State.Returning;
        }

        // Let go mid-flight (stolen or no valid target). We DON'T call OnDragBegin/End
        // ownership tricks — if the player grabbed it, their controller owns the drag now;
        // if it just slipped, ending the drag is correct.
        private void ReleaseCarried(bool stolen)
        {
            if (_carried != null && !stolen)
                _carried.OnDragEnd();

            _carried = null;
            _target = null;
            _tugTimer = 0f;
            OnLostItem();
        }

        private void OnLostItem()
        {
            // We do NOT spend a charge for a failed/stolen delivery — only successful
            // Deliver() burns one. Give up on this item and go back to idle-bounce.
            _carried = null;
            _seekTarget = null;
            _target = null;
            _state = _chargesLeft > 0 ? State.Idle : State.Returning;
        }

        // ---------------------------------------------------------------- SEARCH
        // Find the nearest loose item whose type is routed AND that isn't already held by
        // the player or blocked, within searchRadius.
        private Item FindAssignedLooseItem()
        {
            if (fab == null) return null;

            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = itemLayers,
                useTriggers = false,
            };

            int count = Physics2D.OverlapCircle(_rb.position, searchRadius, filter, _hits);

            Item best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i] == null) continue;
                var item = _hits[i].GetComponentInParent<Item>();
                if (item == null || !CanTake(item)) continue;

                float sqr = ((Vector2)item.Transform.position - _rb.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = item; }
            }
            return best;
        }

        // An item is takeable if: it's routed to a live destination, it's not currently
        // being dragged (by the player or another drone), and it isn't pickup-blocked...
        // actually the pickup filter only gates the HAND, so we ignore it here — drones
        // are explicitly allowed to move blocked types (per PickupFilterService's docs).
        private bool CanTake(Item item)
        {
            if (item == null || item.type == null) return false;
            if (item.IsDragging) return false;                       // someone's holding it
            if (fab == null || !fab.Config.IsRouted(item.type.GetType())) return false;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, searchRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, grabRadius);
        }
    }
}