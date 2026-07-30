using UnityEngine;

namespace CraneMachine
{
    // Ticks when an item enters the leaf blower's zone. Put on the LeafBlower object.
    // (The blower also plays a continuous SFX via its own assigned SfxSource while blowing.)
    [RequireComponent(typeof(SfxSource))]
    public class LeafBlowerSfx : MonoBehaviour
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
