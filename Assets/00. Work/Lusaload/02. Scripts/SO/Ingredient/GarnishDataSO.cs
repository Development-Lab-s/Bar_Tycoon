using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    [CreateAssetMenu(fileName = "Garnish SO", menuName = "Alcohol/Garnish", order = 2)]
    public class GarnishDataSO : BaseAlcoholDataSO
    {
#if UNITY_EDITOR
        private void Reset() { category = IngredientCategory.Garnish; }
#endif
    }
}
