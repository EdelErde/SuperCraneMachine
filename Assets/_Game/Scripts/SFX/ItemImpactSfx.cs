using UnityEngine;

namespace CraneMachine
{
    // Plays when the item lands/collides hard enough. Put on the Item root.
    // Assign its own SfxSource (on a child) so it has an independent clip pool.
    public class ItemImpactSfx : MonoBehaviour
    {
        [SerializeField] private SfxSource impactSfx;
        [SerializeField] private float minImpactSpeed = 2f;
        [SerializeField] private float cooldown = 0.12f;

        private float _next;

        private void OnCollisionEnter2D(Collision2D c)
        {
            if (impactSfx == null) return;
            if (Time.time < _next) return;
            if (c.relativeVelocity.magnitude < minImpactSpeed) return;
            impactSfx.Play();
            _next = Time.time + cooldown;
        }
    }
}