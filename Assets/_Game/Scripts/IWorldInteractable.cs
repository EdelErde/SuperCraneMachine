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
        bool IsDragging { get; }
        float Strain { get; }
        void OnDragBegin();
        void OnDrag(Vector2 worldPoint);
        void OnDragEnd();
    }
}