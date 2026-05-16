using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    // 음료 재료 SO. 생성 시 카테고리를 Drink로 자동 설정
    [CreateAssetMenu(fileName = "Drink SO", menuName = "Alcohol/Drink", order = 1)]
    public class DrinkDataSO : BaseAlcoholDataSO
    {
#if UNITY_EDITOR
        // SO를 처음 생성할 때 카테고리 기본값을 Drink로 고정
        private void Reset() { category = IngredientCategory.Drink; }
#endif
    }
}
