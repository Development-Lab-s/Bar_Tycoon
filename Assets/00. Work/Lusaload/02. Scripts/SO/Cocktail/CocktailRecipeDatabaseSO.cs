using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    [CreateAssetMenu(fileName = "CocktailDataBase", menuName = "Alcohol/CocktailDataBaseSO", order = 0)]
    public class CocktailRecipeDatabaseSO : ScriptableObject
    {
        public List<CocktailRecipeSO> recipes = new();
    }
}