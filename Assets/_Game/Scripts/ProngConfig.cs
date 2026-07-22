using System;
using UnityEngine;

namespace CraneMachine
{
    [Serializable]
    public class ProngConfig
    {
        public float openAngle = 40f;
        public float closedAngle = 0f;
        public float motorForce = 100f;
        public float motorSpeed = 200f;
    }
}