using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace CraneMachine
{
    // One column in the Drone Setup window: a destination (or the Unassigned column).
    // Drop a DroneSetupIcon here to route that item type to this column's destination.
    // Mirrors SorterDialogDropZone, but the destination id is configured at runtime by
    // the controller instead of being a fixed enum, since columns are generated per
    // DroneDestination in the scene.
    public class DroneSetupDropZone : MonoBehaviour, IDropHandler
    {
        [Tooltip("Where dropped icons get reparented — usually this object's own list area " +
                 "(with a layout group).")]
        [SerializeField] private RectTransform listParent;
        [Tooltip("Optional label showing the destination's display name.")]
        [SerializeField] private TMP_Text label;

        private DroneSetupController _controller;
        private string _destinationId = "";

        public string DestinationId => _destinationId;
        public RectTransform ListParent => listParent != null ? listParent : (RectTransform)transform;

        // Called by the controller when it builds/points this column at a destination.
        public void Configure(DroneSetupController controller, string destinationId, string displayName)
        {
            _controller = controller;
            _destinationId = destinationId ?? "";
            if (label != null) label.text = displayName;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var dragged = eventData.pointerDrag;
            if (dragged == null) return;

            var icon = dragged.GetComponent<DroneSetupIcon>();
            if (icon == null || icon.Type == null) return;

            icon.transform.SetParent(ListParent, worldPositionStays: false);

            if (_controller != null) _controller.HandleIconDropped(icon, _destinationId);
        }
    }
}