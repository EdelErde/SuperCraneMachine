using UnityEngine;

namespace CraneMachine
{
    public class ClawController : MonoBehaviour
    {
        public enum State { Sweeping, Lowering, Grabbing, Raising, MovingToDrop, Dropping, Returning }

        [Header("References")]
        [SerializeField] private Transform clawBody;
        [SerializeField] private ClawProng[] prongs;

        [Header("Horizontal sweep")]
        [SerializeField] private float minX = -6f;
        [SerializeField] private float maxX = 3.2f;
        [SerializeField] private float sweepSpeed = 2.5f;

        [Header("Vertical")]
        [SerializeField] private float topY = 4f;
        [SerializeField] private float bottomY = 0.5f;
        [SerializeField] private float verticalSpeed = 3f;

        [Header("Drop")]
        [SerializeField] private float dropX = 5.2f;

        [Header("Timing")]
        [SerializeField] private float grabHold = 0.4f;
        [SerializeField] private float dropHold = 0.5f;

        public State Current { get; private set; } = State.Sweeping;

        private int _sweepDir = 1;
        private float _timer;

        // Live values from the stat system (fall back to serialized if service missing).
        private float SweepSpeed =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.ClawSweepSpeed) : sweepSpeed;
        private float VerticalSpeed =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.ClawVerticalSpeed) : verticalSpeed;

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
                Current = State.Lowering;
        }

        private void Grab()
        {
            CloseProngs();
            _timer += Time.deltaTime;
            if (_timer >= grabHold)
            {
                _timer = 0f;
                Current = State.Raising;
            }
        }

        private void MoveToDrop()
        {
            MoveTowardX(dropX);
            if (Mathf.Abs(clawBody.localPosition.x - dropX) < 0.05f)
                Current = State.Dropping;
        }

        private void Drop()
        {
            OpenProngs();
            _timer += Time.deltaTime;
            if (_timer >= dropHold)
            {
                _timer = 0f;
                Current = State.Returning;
            }
        }

        private void Return()
        {
            float center = (minX + maxX) * 0.5f;
            MoveTowardX(center);
            if (Mathf.Abs(clawBody.localPosition.x - center) < 0.05f)
            {
                _sweepDir = 1;
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