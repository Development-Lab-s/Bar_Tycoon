using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    [CreateAssetMenu(fileName = "CocktailRecipe", menuName = "Alcohol/CocktailRecipeSO", order = 0)]
    public class CocktailRecipeSO : ScriptableObject
    {
        public string cocktailName;
        public Sprite cocktailIcon;

        [Range(0, 5)] public int bitterness; // 쓴맛
        [Range(0, 5)] public int sweetness;  // 단맛
        [Range(0, 5)] public int sourness;   // 신맛

        [TextArea(2, 5)] public string description; // 설명

        public List<BaseAlcoholDataSO> cocktailRecipeList;
    }
}