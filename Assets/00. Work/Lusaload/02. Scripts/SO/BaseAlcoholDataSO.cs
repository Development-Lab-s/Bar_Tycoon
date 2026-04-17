using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    [CreateAssetMenu(fileName = "Alcohol SO", menuName = "Alcohol/Base Alcohol", order = 0)]
    public class BaseAlcoholDataSO : ScriptableObject
    {
        public string alcoholName;
        public Sprite alcoholImage;
    }
}