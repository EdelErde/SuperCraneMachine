using UnityEngine;

namespace CraneMachine
{
    // Continuous roll/scrape loop whose volume tracks the item's speed. Put on the Item prefab.
    // Uses its own looping AudioSource (separate from SfxSource one-shots).
    [RequireComponent(typeof(Rigidbody2D))]
    public class ItemMovementSfx : MonoBehaviour
    {
        [SerializeField] private AudioSource loopSource;   // Loop = ON, Play On Awake = OFF
        [SerializeField] private float minSpeed = 0.5f;    // below this: silent
        [SerializeField] private float maxSpeed = 6f;      // at/above this: full volume
        [SerializeField, Range(0f, 1f)] private float maxVolume = 0.5f;
        [SerializeField] private float fade = 8f;

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (loopSource != null) { loopSource.loop = true; loopSource.playOnAwake = false; }
        }

        private void OnEnable()  { if (loopSource != null) { loopSource.volume = 0f; loopSource.Play(); } }
        private void OnDisable() { if (loopSource != null) loopSource.Stop(); }

        private void Update()
        {
            if (loopSource == null) return;
            float speed = _rb.linearVelocity.magnitude;
            float t = Mathf.InverseLerp(minSpeed, maxSpeed, speed);
            float targetVol = t * maxVolume;
            loopSource.volume = Mathf.MoveTowards(loopSource.volume, targetVol, fade * Time.deltaTime);
        }
    }
}