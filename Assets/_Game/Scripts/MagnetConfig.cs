using System;
using UnityEngine;

namespace CraneMachine
{
    [Serializable]
    public class MagnetConfig
    {
        [Header("Horizontal sweep")]
        public float minX = -6.2f;
        public float maxX = 3.2f;
        public float sweepSpeed = 2.5f;

        [Header("Vertical")]
        public float topY = 4.6f;
        public float bottomY = 0.8f;
        public float verticalSpeed = 3f;

        [Header("Drop")]
        public float dropX = 7.2f;

        [Header("Timing")]
        [Tooltip("Seconds the magnet stays engaged before lifting.")]
        public float grabHold = 0.4f;
        public float dropHold = 0.5f;

        [Header("Magnet")]
        [Tooltip("Fallback square width (world units) if the MagnetRange stat is unavailable.")]
        public float magnetRange = 1.5f;
        [Tooltip("World-space width of the magnet sprite when its localScale.x is 1. " +
                 "Used to stretch the sprite so it visually matches MagnetRange.")]
        public float spriteReferenceWidth = 1.5f;
        [Tooltip("Thin detection strip at the tip: how far below magnetTip it reaches. " +
                 "Keep small — this only decides when the magnet stops descending (on contact).")]
        public float detectionDepth = 0.2f;
        [Tooltip("Pickup zone depth (fallback): how far below magnetTip the magnet grabs once engaged. " +
                 "Overridden by the MagnetDepth stat when available.")]
        public float pickupDepth = 1.5f;
        [Tooltip("Stop descending when the top of the detection square is this far above a touched item.")]
        public float contactPadding = 0.05f;

        [Header("Magnetic attraction")]
        [Tooltip("Base pull strength toward the tip. Higher = snappier, harder to pull off.")]
        public float attractForce = 40f;
        [Tooltip("Softening distance for the inverse-distance falloff (avoids infinite force at the tip).")]
        public float attractSoftening = 0.35f;
        [Tooltip("Max pull force applied to a single item (clamps the inverse-distance spike).")]
        public float attractMaxForce = 120f;
        [Tooltip("Velocity damping on held items so they settle at the tip instead of orbiting.")]
        public float holdDamping = 6f;
        [Tooltip("If the player's drag force exceeds magnet pull by this factor, the item breaks free.")]
        public float breakFreeFactor = 1.1f;
        [Tooltip("Once an item is within this distance of the tip it counts as 'stuck' (for clumping).")]
        public float stickDistance = 0.25f;
    }
}