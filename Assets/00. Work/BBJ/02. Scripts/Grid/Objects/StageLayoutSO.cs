using System.Collections.Generic;
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [CreateAssetMenu(fileName = "StageLayout", menuName = "GridSystem/StageLayout")]
    public class StageLayoutSO : ScriptableObject
    {
        [Tooltip("배치된 장애물 목록 (구조체 리스트 - 별도 파일 없이 이 SO에 직렬화됨)")]
        public List<PlacedObstacleEntry> entries = new List<PlacedObstacleEntry>();
    }

}

