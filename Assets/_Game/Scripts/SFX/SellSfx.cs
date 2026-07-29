using UnityEngine;

namespace CraneMachine
{
    // Plays when an item is sold. Put on the SellService object.
    [RequireComponent(typeof(SfxSource))]
    public class SellSfx : MonoBehaviour
    {
        [SerializeField] private SellService sellService;
        private SfxSource _sfx;

        private void Awake()
        {
            _sfx = GetComponent<SfxSource>();
            if (sellService == null) sellService = GetComponent<SellService>();
        }
        private void OnEnable()  { if (sellService != null) sellService.OnItemSold += OnSold; }
        private void OnDisable() { if (sellService != null) sellService.OnItemSold -= OnSold; }

        private void OnSold(int amount, Vector3 where) => _sfx.Play();
    }
}
