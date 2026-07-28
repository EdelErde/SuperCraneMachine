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
        [Tooltip("Spring stiffness pulling items to the magnet face. Higher = snappier, firmer hold.")]
        public float holdStiffness = 90f;
        [Tooltip("Damping ratio. 1 = critically damped (snaps to place, no overshoot). " +
                 "Slightly below 1 allows a tiny sway; above 1 is sluggish.")]
        public float holdDampingRatio = 1f;
        [Tooltip("Once an item is within this distance of its lane target, it's eligible to settle.")]
        public float settleDistance = 0.06f;
        [Tooltip("...and moving slower than this, so residual jitter gets zeroed out.")]
        public float settleSpeed = 0.4f;
        [Tooltip("How hard to snap onto the exact lane target once settled (0..1 per step).")]
        public float settleSnap = 0.4f;
        [Tooltip("Scales HandStrength into pull-off force. Higher = easier to yank items off.")]
        public float handPullScale = 60f;
        [Tooltip("If the player's drag exceeds the magnet's spring grip by this factor, the item breaks free.")]
        public float breakFreeFactor = 1.1f;
    }
}