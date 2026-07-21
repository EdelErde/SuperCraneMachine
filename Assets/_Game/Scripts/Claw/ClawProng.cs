using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(HingeJoint))]
    public class ClawProng : MonoBehaviour
    {
        [SerializeField] private float openAngle = 40f;
        [SerializeField] private float closedAngle = 0f;
        [SerializeField] private float motorForce = 100f;
        [SerializeField] private float motorSpeed = 200f;

        private HingeJoint _hinge;

        private float Force =>
            ServiceLocator.StatService != null ? ServiceLocator.StatService.GameValue(GameStat.GrabStrength) : motorForce;

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