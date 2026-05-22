using System;
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [Serializable]
    public struct PlacedObstacleEntry
    {
        public Vector2Int cellIndex;
        public ObjectDataSO obstacleData;
        public bool flipX;
    }
}
