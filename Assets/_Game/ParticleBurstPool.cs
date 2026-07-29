using System.Collections.Generic;
using UnityEngine;

namespace CraneMachine
{
    public class ParticleBurstPool : MonoBehaviour
    {
        [Tooltip("A ParticleSystem prefab set to Play On Awake = OFF, Looping = OFF.")]
        [SerializeField] private ParticleSystem prefab;
        [SerializeField] private int prewarm = 4;

        private readonly Queue<ParticleSystem> _free = new Queue<ParticleSystem>();
        private readonly List<ParticleSystem> _busy = new List<ParticleSystem>();

        private void Awake()
        {
            ServiceLocator.Particles = this;
            for (int i = 0; i < prewarm; i++) _free.Enqueue(Create());
        }
        private ParticleSystem Create()
        {
            var ps = Instantiate(prefab, transform);
            ps.gameObject.SetActive(false);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            return ps;
        }

        public void Play(Vector2 position, Vector2 normal = default, float scale = 1f)
        {
            if (prefab == null) return;

            var ps = _free.Count > 0 ? _free.Dequeue() : Create();
            var t = ps.transform;
            t.position = position;
            if (normal != default) t.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            t.localScale = Vector3.one * scale;

            ps.gameObject.SetActive(true);
            ps.Clear();
            ps.Play();
            _busy.Add(ps);
        }

        private void Update()
        {
            for (int i = _busy.Count - 1; i >= 0; i--)
            {
                var ps = _busy[i];
                if (ps == null) { _busy.RemoveAt(i); continue; }
                if (!ps.IsAlive(true))
                {
                    ps.gameObject.SetActive(false);
                    _busy.RemoveAt(i);
                    _free.Enqueue(ps);
                }
            }
        }
    }
}