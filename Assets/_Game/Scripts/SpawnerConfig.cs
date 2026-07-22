using System;
using UnityEngine;

namespace CraneMachine
{
    [Serializable]
    public class SpawnerConfig
    {
        public float spawnInterval = 3f;
        public int itemsPerDrop = 1;
        public float intervalJitter = 0.15f;
        public int maxLiveItems = 4;
        public int initialBurst = 4;
        public bool spawnOnStart = true;
    }
}