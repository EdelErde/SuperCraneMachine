using UnityEngine;

namespace CraneMachine
{
    // Plays when an item drops into the sell hole. Put on the SellHole object.
    // (Separate from SellSfx so the "drop in" and the "cha-ching" can differ.)
    [RequireComponent(typeof(SfxSource))]
    public class SellHoleSfx : MonoBehaviour
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
