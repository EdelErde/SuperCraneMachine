using UnityEngine;

namespace CraneMachine
{
    // Plays when a new item spawns. Put on the ItemSpawner object.
    [RequireComponent(typeof(SfxSource))]
    public class SpawnSfx : MonoBehaviour
    {
        [SerializeField] private ItemSpawner spawner;
        private SfxSource _sfx;

        private void Awake()
        {
            _sfx = GetComponent<SfxSource>();
            if (spawner == null) spawner = GetComponent<ItemSpawner>();
        }
        private void OnEnable()  { if (spawner != null) spawner.OnSpawned += _sfx.Play; }
        private void OnDisable() { if (spawner != null) spawner.OnSpawned -= _sfx.Play; }
    }
}
