using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    [CreateAssetMenu(fileName = "Drink SO", menuName = "Alcohol/Drink", order = 1)]
    public class DrinkDataSO : BaseAlcoholDataSO
    {
#if UNITY_EDITOR
        private void Reset() { category = IngredientCategory.Drink; }
#endif
    }
}
