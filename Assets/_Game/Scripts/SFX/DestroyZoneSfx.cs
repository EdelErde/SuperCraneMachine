using UnityEngine;

namespace CraneMachine
{
    // Plays when an item falls into a destroy zone. Put on the DestroyZone object.
    [RequireComponent(typeof(SfxSource))]
    public class DestroyZoneSfx : MonoBehaviour
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
