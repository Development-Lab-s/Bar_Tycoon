using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    [CreateAssetMenu(fileName = "CocktailRecipe", menuName = "Alcohol/CocktailRecipeSO", order = 0)]
    public class CocktailRecipeSO : ScriptableObject
    {
        public string cocktailName; // 칵테일 이름
        public Sprite cocktailIcon;
        public List<BaseAlcoholDataSO> cocktailRecipeList;
    }
}