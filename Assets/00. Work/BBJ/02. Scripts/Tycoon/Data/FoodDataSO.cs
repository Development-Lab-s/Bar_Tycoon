using UnityEngine;

namespace BBJ.Tycoon
{
    [CreateAssetMenu(fileName = "FoodData", menuName = "Tycoon/FoodData")]
    public class FoodDataSO : ScriptableObject
    {
        public string FoodName;
        public float  CookTime = 3f;
        public int    Price    = 1000;
        public Sprite Icon;
    }
}
