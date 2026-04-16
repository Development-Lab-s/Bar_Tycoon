using UnityEngine;

namespace PathFind.Obstacle
{
    [CreateAssetMenu(fileName = "ObstacleData", menuName = "PathFind/Obstacle")]
    public class ObstacleData : ScriptableObject
    {
        public GameObject Prefab;
        public bool isWalkable;
        public Vector2Int[] blockedOffsets;

        // 추후 추가, 상호작용이 필요한 Grid
    }
}
