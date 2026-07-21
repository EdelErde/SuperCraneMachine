using UnityEngine;

namespace CraneMachine
{
    public interface IWorldInteractable
    {
        Transform Transform { get; }
    }

    public interface IDraggable : IWorldInteractable
    {
        bool CanDrag { get; }
        void OnDragBegin();
        void OnDrag(Vector2 worldPoint);
        void OnDragEnd();
    }
}