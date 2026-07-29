using UnityEngine;

namespace CraneMachine
{
    // Plays when the item lands/collides hard enough. Put on the Item prefab.
    [RequireComponent(typeof(SfxSource))]
    public class ItemImpactSfx : MonoBehaviour
    {
        [SerializeField] private float minImpactSpeed = 2f;
        [SerializeField] private float cooldown = 0.12f;

        private SfxSource _sfx;
        private float _next;

        private void Awake() => _sfx = GetComponent<SfxSource>();

        private void OnCollisionEnter2D(Collision2D c)
        {
            if (Time.time < _next) return;
            if (c.relativeVelocity.magnitude < minImpactSpeed) return;
            _sfx.Play();
            _next = Time.time + cooldown;
        }
    }
}
