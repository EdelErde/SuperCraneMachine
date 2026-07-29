using UnityEngine;

namespace CraneMachine
{
    // Plays sounds as the magnet changes state (grab / raise / drop). Put on the Magnet object.
    // Polls MagnetController.Current, so no changes to that script are needed.
    public class MagnetSfx : MonoBehaviour
    {
        [SerializeField] private MagnetController magnet;
        [SerializeField] private SfxSource grabSfx;    // when it clamps down
        [SerializeField] private SfxSource raiseSfx;   // when it lifts
        [SerializeField] private SfxSource dropSfx;    // when it releases

        private MagnetController.State _last;

        private void Awake()
        {
            if (magnet == null) magnet = GetComponent<MagnetController>();
            if (magnet != null) _last = magnet.Current;
        }

        private void Update()
        {
            if (magnet == null) return;
            var s = magnet.Current;
            if (s == _last) return;

            switch (s)
            {
                case MagnetController.State.Grabbing: if (grabSfx != null) grabSfx.Play(); break;
                case MagnetController.State.Raising:  if (raiseSfx != null) raiseSfx.Play(); break;
                case MagnetController.State.Dropping: if (dropSfx != null) dropSfx.Play(); break;
            }
            _last = s;
        }
    }
}
