using UnityEngine;

namespace CraneMachine
{
    // Plays when any upgrade is purchased. Put on the UpgradeService object.
    [RequireComponent(typeof(SfxSource))]
    public class BuySfx : MonoBehaviour
    {
        private SfxSource _sfx;
        private bool _ready;

        private void Awake() => _sfx = GetComponent<SfxSource>();

        private void OnEnable()
        {
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged += OnChanged;
        }
        private void OnDisable()
        {
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged -= OnChanged;
        }

        // Skip the one-shot sync event fired on the first frame.
        private void Start() => _ready = true;
        private bool extraReady;

        private void OnChanged()
        {
            if (extraReady) _sfx.Play();
            if (_ready) extraReady = true;

        }
    }
}
