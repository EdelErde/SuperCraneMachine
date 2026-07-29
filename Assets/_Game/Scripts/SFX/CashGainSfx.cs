using UnityEngine;

namespace CraneMachine
{
    // Plays whenever money is earned (not spent). Put on the StatService object.
    [RequireComponent(typeof(SfxSource))]
    public class CashGainSfx : MonoBehaviour
    {
        private SfxSource _sfx;
        private void Awake() => _sfx = GetComponent<SfxSource>();

        private void OnEnable()
        {
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyEarned += OnEarned;
        }
        private void OnDisable()
        {
            if (ServiceLocator.StatService != null)
                ServiceLocator.StatService.OnMoneyEarned -= OnEarned;
        }

        private void OnEarned(int amount) => _sfx.Play();
    }
}
