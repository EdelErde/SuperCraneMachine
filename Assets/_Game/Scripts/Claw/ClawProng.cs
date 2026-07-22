using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(HingeJoint))]
    public class ClawProng : MonoBehaviour
    {
        [SerializeField] private ProngConfig config = new ProngConfig();

        private HingeJoint _hinge;

        private float openAngle => config.openAngle;
        private float closedAngle => config.closedAngle;

        private float Force =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.GrabStrength) : config.motorForce;

        private void Awake() => _hinge = GetComponent<HingeJoint>();

        public void Open()  => DriveTo(openAngle);
        public void Close() => DriveTo(closedAngle);

        private void DriveTo(float targetAngle)
        {
            _hinge.useSpring = true;
            var spring = _hinge.spring;
            spring.targetPosition = targetAngle;
            spring.spring = Force;
            spring.damper = Force * 0.1f;
            _hinge.spring = spring;
        }
    }
}