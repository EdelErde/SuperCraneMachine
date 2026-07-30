using UnityEngine;

namespace CraneMachine
{
    // Ticks when an item enters the sorting machine's intake. Put on the SortingMachine object.
    // (Intake / sort SFX can also be assigned directly on the SortingMachine for finer control.)
    [RequireComponent(typeof(SfxSource))]
    public class SortingMachineSfx : MonoBehaviour
    {
        private SfxSource _sfx;
        private void Awake() => _sfx = GetComponent<SfxSource>();

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<Item>() != null)
                _sfx.Play();
        }
    }
}
