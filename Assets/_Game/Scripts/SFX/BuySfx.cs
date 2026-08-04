using UnityEngine;

namespace CraneMachine
{
    // Plays when any upgrade is actually purchased. Put on the UpgradeService object.
    //
    // OnUpgradesChanged is a general "something about upgrades changed" signal: it also
    // fires once during startup (when the view registers its buttons) and can be raised
    // by non-purchase refreshes. Listening to it directly made the buy sound play when
    // the panel was opened. Instead we watch the purchase COUNT and only play when it
    // actually goes up, so the sound maps one-to-one to real buys.
    [RequireComponent(typeof(SfxSource))]
    public class BuySfx : MonoBehaviour
    {
        private SfxSource _sfx;
        private int _lastPurchases;

        private void Awake() => _sfx = GetComponent<SfxSource>();

        private void OnEnable()
        {
            // Seed to the current total so we never treat the initial sync as a purchase.
            _lastPurchases = CurrentPurchases();
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged += OnChanged;
        }

        private void OnDisable()
        {
            if (ServiceLocator.UpgradeService != null)
                ServiceLocator.UpgradeService.OnUpgradesChanged -= OnChanged;
        }

        private void OnChanged()
        {
            int now = CurrentPurchases();

            // Only a real purchase raises the total. Panel-open / refresh events leave it
            // unchanged, so they stay silent.
            if (now > _lastPurchases) _sfx.Play();

            _lastPurchases = now;
        }

        private static int CurrentPurchases()
            => ServiceLocator.UpgradeService != null
                ? ServiceLocator.UpgradeService.TotalPurchases()
                : 0;
    }
}