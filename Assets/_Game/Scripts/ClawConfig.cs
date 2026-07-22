using System;
using UnityEngine;

namespace CraneMachine
{
    [Serializable]
    public class ClawConfig
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
        public float grabHold = 0.4f;
        public float dropHold = 0.5f;
    }
}