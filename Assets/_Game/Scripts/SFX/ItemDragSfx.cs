using UnityEngine;

namespace CraneMachine
{
    // Plays a pickup sound on grab and a release sound on let-go. Put on the Item prefab.
    // Watches the Item's IsDragging state, so no changes to Item.cs are needed.
    [RequireComponent(typeof(Item))]
    public class ItemDragSfx : MonoBehaviour
    {
        [SerializeField] private SfxSource grabSfx;
        [SerializeField] private SfxSource releaseSfx;

        private Item _item;
        private bool _wasDragging;

        private void Awake() => _item = GetComponent<Item>();

        private void Update()
        {
            bool now = _item.IsDragging;
            if (now && !_wasDragging && grabSfx != null) grabSfx.Play();
            if (!now && _wasDragging && releaseSfx != null) releaseSfx.Play();
            _wasDragging = now;
        }
    }
}
