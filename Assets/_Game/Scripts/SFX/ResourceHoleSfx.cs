using UnityEngine;

namespace CraneMachine
{
    // Plays when an item drops into the resource (second) hole. Put on the ResourceHole object.
    [RequireComponent(typeof(SfxSource))]
    public class ResourceHoleSfx : MonoBehaviour
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
