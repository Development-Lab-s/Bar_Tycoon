using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    // 동일 카테고리의 재료 SO를 묶어 보관하는 리스트 SO
    [CreateAssetMenu(fileName = "Alcohol List SO", menuName = "Alcohol/Alcohol List", order = 0)]
    public class AlcoholListSO : ScriptableObject
    {
        public List<BaseAlcoholDataSO> alcoholList; // 이 리스트에 속한 재료 SO 목록
    }
}