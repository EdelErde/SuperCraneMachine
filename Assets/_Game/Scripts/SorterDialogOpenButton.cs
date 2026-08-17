using UnityEngine;
using UnityEngine.UI;

namespace CraneMachine
{
    // Button placed on/near a SortingMachine that opens its SorterDialogController.
    // Clicking again while open closes it (acts as a toggle), matching the sketch's
    // "click the sorter to open the dialog" flow.
    [RequireComponent(typeof(Button))]
    public class SorterDialogOpenButton : MonoBehaviour
    {
        [Tooltip("Dialog to toggle. Auto-found on this object or a parent if left empty.")]
        [SerializeField] private SorterDialogController dialog;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (dialog == null) dialog = GetComponentInParent<SorterDialogController>();
        }

        private void OnEnable() => _button.onClick.AddListener(HandleClick);

        private void OnDisable() => _button.onClick.RemoveListener(HandleClick);

        private void HandleClick()
        {
            if (dialog != null) dialog.Toggle();
        }
    }
}