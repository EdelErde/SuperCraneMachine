using UnityEngine;

namespace CraneMachine
{
    public class ClawProng : MonoBehaviour
    {
        [SerializeField] private ProngConfig config = new ProngConfig();

        private float _target;
        private float _current;

        private void Awake()
        {
            _current = config.openAngle;
            _target = config.openAngle;
            Apply();
        }

        public void Open() => _target = config.openAngle;
        public void Close() => _target = config.closedAngle;

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;

            _current = Mathf.MoveTowards(_current, _target, config.motorSpeed * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            var e = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(e.x, e.y, _current);
        }
    }
}