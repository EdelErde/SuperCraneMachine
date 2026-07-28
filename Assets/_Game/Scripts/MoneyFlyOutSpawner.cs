using UnityEngine;

namespace CraneMachine
{
    public class MoneyFlyoutSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The MoneyFlyout prefab (world-space TextMeshPro).")]
        [SerializeField] private MoneyFlyout flyoutPrefab;
        [Tooltip("Where flyouts appear. Defaults to this object's transform (put it on the sell hole).")]
        [SerializeField] private Transform spawnPoint;

        [Header("Batching")]
        [Tooltip("Sales within this window are summed into one popup.")]
        [SerializeField] private float batchWindow = 0.2f;

        [Header("Look")]
        [SerializeField] private float flyoutScale = 1f;
        [Tooltip("Random horizontal jitter so stacked popups don't perfectly overlap.")]
        [SerializeField] private float xJitter = 0.15f;

        private bool _windowOpen;
        private float _windowEndsAt;
        private int _batchTotal;
        private Vector3 _batchPos;
        private int _batchCount;

        private void OnEnable()
        {
            if (ServiceLocator.SellService != null)
                ServiceLocator.SellService.OnItemSold += HandleSold;
            else
                Invoke(nameof(TrySubscribe), 0f);
        }

        private void TrySubscribe()
        {
            if (ServiceLocator.SellService != null)
                ServiceLocator.SellService.OnItemSold += HandleSold;
        }

        private void OnDisable()
        {
            if (ServiceLocator.SellService != null)
                ServiceLocator.SellService.OnItemSold -= HandleSold;
        }

        private void HandleSold(int amount, Vector3 worldPos)
        {
            if (!_windowOpen)
            {
                _windowOpen = true;
                _windowEndsAt = Time.time + batchWindow;
                _batchTotal = 0;
                _batchCount = 0;
                _batchPos = Vector3.zero;
            }
            _batchTotal += amount;
            _batchCount++;
            _batchPos += worldPos; 
        }

        private void Update()
        {
            if (!_windowOpen || Time.time < _windowEndsAt) return;
            Flush();
        }

        private void Flush()
        {
            _windowOpen = false;
            if (_batchTotal <= 0 || flyoutPrefab == null) return;

            Vector3 basePos = spawnPoint != null ? spawnPoint.position
                            : (_batchCount > 0 ? _batchPos / _batchCount : transform.position);
            basePos.x += Random.Range(-xJitter, xJitter);

            var fx = Instantiate(flyoutPrefab, basePos, Quaternion.identity);
            fx.Play(_batchTotal, basePos, flyoutScale);
        }

        private void Reset()
        {
            spawnPoint = transform;
        }
    }
}