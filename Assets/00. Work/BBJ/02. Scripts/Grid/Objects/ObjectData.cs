using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [CreateAssetMenu(fileName = "ObstacleData", menuName = "GridSystem/Object")]
    public class ObjectData : ScriptableObject
    {
        public GameObject Prefab;
        // 점유 Grid
        public Vector2Int[] BlockedOffsets;
        // 상호작용 Grid
        public Vector2Int[] InteractOffsets;

        public bool IsWalkable;
        public bool IsInteractable => InteractOffsets.Length != 0;
    }
}