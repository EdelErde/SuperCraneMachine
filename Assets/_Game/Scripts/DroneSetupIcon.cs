using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraneMachine
{
    // One draggable item icon inside the Drone Setup window. Represents an item type
    // currently routed to whichever destination column it's parented under. Drag it onto
    // another column to reassign; a DroneSetupDropZone catches the drop and calls back
    // into DroneSetupController. Straight copy of SorterDialogIcon's behaviour so the two
    // config windows feel identical.
    public class DroneSetupIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image icon;
        [Tooltip("Alpha applied to the icon while being dragged.")]
        [SerializeField] private float dragAlpha = 0.75f;

        private ItemType _type;
        private RectTransform _rect;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Transform _originalParent;
        private Vector2 _originalAnchoredPos;

        public ItemType Type => _type;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Bind(ItemType type, Sprite sprite)
        {
            _type = type;
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalParent = transform.parent;
            _originalAnchoredPos = _rect.anchoredPosition;

            if (_canvas != null) transform.SetParent(_canvas.transform, worldPositionStays: true);

            _canvasGroup.alpha = dragAlpha;
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_canvas == null) return;

            var cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _canvas.transform as RectTransform, eventData.position, cam, out var worldPoint);
            transform.position = worldPoint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            // If no drop zone handled it, snap back to where it started.
            if (transform.parent == _canvas.transform)
            {
                transform.SetParent(_originalParent, worldPositionStays: false);
                _rect.anchoredPosition = _originalAnchoredPos;
            }
        }
    }
}