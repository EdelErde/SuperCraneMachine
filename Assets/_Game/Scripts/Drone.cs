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
        [Tooltip("Layers loose items live on (leave Everything if unsure). Used to scan the " +
                 "drone's assigned screen area for items.")]
        [SerializeField] private LayerMask itemLayers = ~0;
        [Tooltip("Vertical offset of the carried item below the drone (world units).")]
        [SerializeField] private float carryDrop = 0.35f;
        [Tooltip("Safety cap on how many colliders one screen scan can return. Raise only if " +
                 "a screen can hold a truly huge number of loose items at once.")]
        [SerializeField] private int maxScanHits = 128;

        [Header("Tug-of-war (stealing)")]
        [Tooltip("How hard the carried item pulls back on the drone. 0 = drone unaffected " +
                 "by the item; higher = drone gets yanked more when you steal.")]
        [SerializeField] private float tugFactor = 6f;
        [Tooltip("Seconds the drone keeps trying to hold an item that's being pulled away " +
                 "before it gives up (the 'brief tug' window).")]
        [SerializeField] private float tugGiveUpTime = 0.35f;
        [Tooltip("HARD outer cutoff: if the carried item ends up further than this from the " +
                 "drone body itself, drop it instantly (no tug window). Safety net for the " +
                 "item being flung far — keep it a bit LARGER than the item's Drone Carry " +
                 "Leash, which is the normal, softer 'it's being pulled away' trigger.")]
        [SerializeField] private float snapDistance = 3f;

        [Header("Collision (walls)")]
        [Tooltip("Layers the drone should physically bump into instead of flying through " +
                 "(walls, machine bodies, the ceiling). Loose items should NOT be on these " +
                 "layers. If left empty the drone falls back to 'everything except itemLayers'.")]
        [SerializeField] private LayerMask wallLayers = 0;
        [Tooltip("Radius of the body collider the drone creates for itself at runtime if it " +
                 "has no Collider2D. Roughly the drone sprite's half-width in world units.")]
        [SerializeField] private float bodyRadius = 0.25f;

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
        private ScreenId _screen;            // which screen this drone searches for items on

        private Collider2D[] _hits;

        public State CurrentState => _state;
        public int ChargesLeft => _chargesLeft;
        public ScreenId Screen => _screen;
        public bool IsBusy => _state == State.Seeking || state == StateAliasCarrying();

        // tiny helper to keep the expression above readable without a second enum compare typo
        private State StateAliasCarrying() => State.Carrying;
        private State state => _state;

        // Called by the fab right after Instantiate.
        public void Init(DroneFab owner, int charges, Vector2 idlePoint, ScreenId screen)
        {
            fab = owner;
            _screen = screen;
            _chargesLeft = Mathf.Max(1, charges);
            _homeIdle = idlePoint;
        }

        // Reassign this drone to a different screen at runtime. If it's mid-carry, it still
        // finishes delivering its current item; the new screen only changes where it looks
        // for the NEXT item. (Hook this up to whatever UI you add for moving drones around.)
        public void AssignScreen(ScreenId screen) => _screen = screen;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;          // helicopters hover; no gravity

            // Make the body actually collide with walls. A dynamic Rigidbody2D with no
            // collider passes through everything, which is why the drone tunnelled through
            // walls. We ensure a (non-trigger) collider exists and set the body up to push
            // out of solids cleanly. Continuous detection stops fast drones from tunnelling
            // thin walls; freezeRotation keeps the sprite upright when it bumps something.
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.freezeRotation = true;

            var col = GetComponent<Collider2D>();
            if (col == null)
            {
                var circle = gameObject.AddComponent<CircleCollider2D>();
                circle.radius = Mathf.Max(0.01f, bodyRadius);
                col = circle;
            }
            col.isTrigger = false;          // solid, so physics resolves wall overlaps

            _bobPhase = Random.value * 100f;
            if (_homeIdle.sqrMagnitude < 0.0001f) _homeIdle = _rb.position;
        }

        // The layer mask treated as "solid" for steering + collision reasoning. Prefer the
        // explicit wallLayers; if it's empty, fall back to "everything the items aren't on."
        private int SolidMask => wallLayers.value != 0 ? wallLayers.value : ~itemLayers.value;

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

            // STEAL DETECTION. When the player grabs the item, Item.OnDragBegin clears its
            // drone-carry flag and the hand takes over — so from our side the tell is simply
            // "the item is no longer in drone-carry under us." We detect that as: it stopped
            // being dragged at all (slipped), OR it's trailing further than its own carry
            // leash (someone is pulling it away). Either way we let go. This replaces the old
            // pixel-fragile drift math that mis-fired at small on-screen scale.
            // Hard cutoff: if the item is ever this far from the drone itself, drop it
            // instantly (no tug window) — a safety net for the item getting flung far.
            float hardDist = Vector2.Distance(_carried.Transform.position, _rb.position);
            if (hardDist > snapDistance)
            {
                Vector2 yank = ((Vector2)_carried.Transform.position - _rb.position);
                _rb.AddForce(yank * tugFactor, ForceMode2D.Impulse);
                ReleaseCarried(stolen: true);
                return;
            }

            bool stolenOrSlipped =
                !_carried.IsDragging ||
                _carried.DragError > _carried.DroneCarryLeash;

            if (stolenOrSlipped)
            {
                _tugTimer += Time.fixedDeltaTime;
                // Brief tug window: give the player a moment where the drone lurches after
                // the item before it fully lets go — the "juicy" tug-of-war.
                Vector2 pull = ((Vector2)_carried.Transform.position - _rb.position);
                _rb.AddForce(pull * tugFactor, ForceMode2D.Impulse);

                if (_tugTimer >= tugGiveUpTime || !_carried.IsDragging)
                {
                    ReleaseCarried(stolen: true);
                    return;
                }
            }
            else
            {
                _tugTimer = 0f;
            }

            // Drive the carry point to just below the drone. The item's own drone-carry
            // physics pull it there firmly; we do NOT add an opposing force to ourselves
            // here (that was the drone "struggling with" its own cargo). A light weigh-down
            // is applied instead, scaled small so it reads as heft without stalling flight.
            Vector2 carryPoint = _rb.position + Vector2.up * -carryDrop;
            _carried.OnDrag(carryPoint);

            Vector2 lag = ((Vector2)_carried.Transform.position - carryPoint);
            _rb.AddForce(lag * (tugFactor * 0.25f));   // gentle heft, not a fight

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

            // Steer away from solid geometry (walls/machine bodies), not loose items.
            int solidMask = SolidMask;

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

            // Carry it with the drone-specific grip: firm tracking, no silent slip, but still
            // steal-able by the player. (NOT the plain hand OnDragBegin — that used the hand's
            // slip leash, which a moving drone trips instantly, dropping the item mid-air.)
            item.BeginDroneCarry(item.Transform.position);
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
        // Find the nearest loose item, ON THIS DRONE'S SCREEN, whose type is routed and
        // carry-unlocked and that isn't already held. No search radius: the drone considers
        // every item within its assigned screen's area (ScreenArea), however far. "Nearest"
        // only breaks ties between otherwise-equal candidates so it grabs sensibly.
        private Item FindAssignedLooseItem()
        {
            if (fab == null) return null;

            var area = ScreenArea.For(_screen);
            if (area == null) return null;   // no ScreenArea authored for this screen yet

            if (_hits == null || _hits.Length != Mathf.Max(8, maxScanHits))
                _hits = new Collider2D[Mathf.Max(8, maxScanHits)];

            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = itemLayers,
                useTriggers = false,
            };

            Bounds b = area.Bounds;
            int count = Physics2D.OverlapBox(b.center, b.size, 0f, filter, _hits);

            Item best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i] == null) continue;
                var item = _hits[i].GetComponentInParent<Item>();
                if (item == null || !CanTake(item)) continue;

                // Definitive membership test — the OverlapBox is a broad-phase filter; this
                // confirms the item's actual position is inside the screen rectangle.
                if (!area.Contains(item.Transform.position)) continue;

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

            // Carry gating: drones may only haul item types whose DroneCarry stat is unlocked
            // (Fuel is on by default; others via UnlockDroneCarryUpgrade). An assigned-but-
            // locked type is simply ignored until its hauling upgrade is bought.
            if (ServiceLocator.StatService != null &&
                !ServiceLocator.StatService.DroneCanCarry(item.type.GetType()))
                return false;

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            // Show the drone's working area (its screen) if available at runtime, plus the
            // grip radius. No search radius any more — the whole screen is in range.
            if (Application.isPlaying)
            {
                var area = ScreenArea.For(_screen);
                if (area != null)
                {
                    Bounds b = area.Bounds;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube(b.center, b.size);
                }
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, grabRadius);
        }
    }
}