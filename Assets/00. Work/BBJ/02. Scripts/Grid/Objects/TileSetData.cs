using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [CreateAssetMenu(fileName = "ObstacleData", menuName = "GridSystem/Object")]
    public class TileSetData : ScriptableObject
    {
        public Vector2Int[] BlockedOffsets;
        public bool IsWalkable;
    }
}
