using System;
using UnityEngine;

namespace CraneMachine
{
    [Serializable]
    public class DragConfig
    {
        [Tooltip("How strongly the item is pulled toward the pointer.")]
        public float dragForce = 30f;
        [Tooltip("Higher = less overshoot/wobble while dragging.")]
        public float dragDamping = 6f;
    }
}