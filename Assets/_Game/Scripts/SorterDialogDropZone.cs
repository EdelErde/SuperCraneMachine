using UnityEngine;
using UnityEngine.EventSystems;

namespace CraneMachine
{
    // Drop target for one side of the Sorter Dialog. Place on the Hole A list root and
    // the Hole B list root, each with its own 'exit' value. When a SorterDialogIcon is
    // dropped here, it's reparented into this list and the controller is notified so it
    // can update the SortingConfig and re-run layout.
    public class SorterDialogDropZone : MonoBehaviour, IDropHandler
    {
        [SerializeField] private SortExit exit;
        [Tooltip("Where dropped icons get reparented — usually this object's own RectTransform (with a layout group).")]
        [SerializeField] private RectTransform listParent;
        [SerializeField] private SorterDialogController controller;

        public SortExit Exit => exit;
        public RectTransform ListParent => listParent != null ? listParent : (RectTransform)transform;

        public void OnDrop(PointerEventData eventData)
        {
            var dragged = eventData.pointerDrag;
            if (dragged == null) return;

            var icon = dragged.GetComponent<SorterDialogIcon>();
            if (icon == null || icon.Type == null) return;

            icon.transform.SetParent(ListParent, worldPositionStays: false);

            if (controller != null) controller.HandleIconDropped(icon, exit);
        }
    }
}