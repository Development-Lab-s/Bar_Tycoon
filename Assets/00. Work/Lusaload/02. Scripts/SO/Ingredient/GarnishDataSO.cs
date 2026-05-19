using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    // 가니시·부재료 SO. 생성 시 카테고리를 Garnish로 자동 설정
    [CreateAssetMenu(fileName = "Garnish SO", menuName = "Alcohol/Garnish", order = 2)]
    public class GarnishDataSO : BaseAlcoholDataSO
    {
#if UNITY_EDITOR
        // SO를 처음 생성할 때 카테고리 기본값을 Garnish로 고정
        private void Reset() { category = IngredientCategory.Garnish; }
#endif
    }
}
