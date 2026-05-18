using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    // 모든 재료 SO의 기반 데이터. 이름·이미지·카테고리를 공통으로 보유
    [CreateAssetMenu(fileName = "Alcohol SO", menuName = "Alcohol/Base Alcohol", order = 0)]
    public class BaseAlcoholDataSO : ScriptableObject
    {
        public string alcoholName;           // 재료 한글 이름
        public Sprite alcoholImage;          // 재료 UI 이미지
        public IngredientCategory category;  // 기주 / 음료 / 가니시 구분
    }
}