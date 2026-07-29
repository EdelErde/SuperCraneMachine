using UnityEngine;

namespace CraneMachine
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ItemLandingParticles : MonoBehaviour
    {
        [Tooltip("Minimum impact speed before particles play.")]
        [SerializeField] private float minImpactSpeed = 2f;
        [Tooltip("Layers that count as ground/surfaces. Leave as Everything to react to all.")]
        [SerializeField] private LayerMask groundMask = ~0;
        [Tooltip("Stops repeat bursts from a single bouncy landing.")]
        [SerializeField] private float cooldown = 0.15f;

        private float _nextTime;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Time.time < _nextTime) return;
            if ((groundMask.value & (1 << collision.collider.gameObject.layer)) == 0) return;
            if (collision.relativeVelocity.magnitude < minImpactSpeed) return;

            if (ServiceLocator.Particles == null) return;

            var contact = collision.GetContact(0);
            float scale = Mathf.Clamp(collision.relativeVelocity.magnitude / minImpactSpeed, 1f, 3f);
            ServiceLocator.Particles.Play(contact.point, contact.normal, scale);

            _nextTime = Time.time + cooldown;
        }
    }
}