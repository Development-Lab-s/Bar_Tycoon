using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    [CreateAssetMenu(fileName = "Alcohol List SO", menuName = "Alcohol/Alcohol List", order = 0)]
    public class AlcoholListSO : ScriptableObject
    {
        public List<BaseAlcoholDataSO> alcoholList;
    }
}