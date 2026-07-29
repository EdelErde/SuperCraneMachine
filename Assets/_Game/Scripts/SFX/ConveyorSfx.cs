using UnityEngine;

namespace CraneMachine
{
    // Small tick each time an item steps onto the belt. Put on the ConveyorBelt object.
    // (Belt trigger already fires OnTriggerEnter2D for items; this mirrors that check.)
    [RequireComponent(typeof(SfxSource))]
    public class ConveyorSfx : MonoBehaviour
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
