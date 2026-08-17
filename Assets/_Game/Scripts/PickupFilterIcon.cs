using System;
using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // One icon in the Pickup Filter grid. Click toggles whether this item type can be
    // picked up with the mouse drag mechanic; blocked types show dimmed/greyed out.
    // Mirrors the active/inactive tint convention used elsewhere (FuelConsumerRow,
    // UpgradeButton): full color = pickup-able, dimmed = blocked.
    [RequireComponent(typeof(Button))]
    public class PickupFilterIcon : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [Tooltip("Optional: dims/tints the whole icon when blocked. Defaults to the icon Image if left empty.")]
        [SerializeField] private Image tintTarget;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color blockedColor = new Color(0.5f, 0.5f, 0.5f);
        [Tooltip("Optional overlay (e.g. a cross/lock icon) shown only while blocked.")]
        [SerializeField] private GameObject blockedOverlay;

        private Button _button;
        private ItemType _type;

        public ItemType Type => _type;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (tintTarget == null) tintTarget = icon;
        }

        public void Bind(ItemType type, Sprite sprite)
        {
            _type = type;

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }

            _button.onClick.RemoveListener(HandleClick);
            _button.onClick.AddListener(HandleClick);

            Refresh();
        }

        private void OnEnable() => Refresh();

        private void OnDestroy() => _button.onClick.RemoveListener(HandleClick);

        private void HandleClick()
        {
            if (_type == null || ServiceLocator.PickupFilter == null) return;
            ServiceLocator.PickupFilter.Toggle(_type);
            Refresh();
        }

        private void Refresh()
        {
            if (_type == null) return;

            bool blocked = ServiceLocator.PickupFilter != null && ServiceLocator.PickupFilter.IsBlocked(_type);

            if (tintTarget != null) tintTarget.color = blocked ? blockedColor : activeColor;
            if (blockedOverlay != null) blockedOverlay.SetActive(blocked);
        }
    }
}