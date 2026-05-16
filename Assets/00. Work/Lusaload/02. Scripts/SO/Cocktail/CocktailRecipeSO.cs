using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    // 한 가지 칵테일의 이름·이미지·맛 프로필·레시피 재료를 보유하는 SO
    [CreateAssetMenu(fileName = "CocktailRecipe", menuName = "Alcohol/CocktailRecipeSO", order = 0)]
    public class CocktailRecipeSO : ScriptableObject
    {
        public string cocktailName;          // 칵테일 한글 이름
        public Sprite cocktailIcon;          // 칵테일 UI 아이콘

        [Range(0, 5)] public int bitterness; // 쓴맛 (0~5)
        [Range(0, 5)] public int sweetness;  // 단맛 (0~5)
        [Range(0, 5)] public int sourness;   // 신맛 (0~5)

        [TextArea(2, 5)] public string description; // 칵테일 특징 설명

        // 이 칵테일을 완성하기 위해 셰이커에 넣어야 할 재료 목록 (순서 포함)
        public List<BaseAlcoholDataSO> cocktailRecipeList;
    }
}