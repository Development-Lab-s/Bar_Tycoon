using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    // 게임에 등록된 모든 칵테일 레시피 SO를 한 곳에서 관리하는 데이터베이스 SO
    [CreateAssetMenu(fileName = "CocktailDataBase", menuName = "Alcohol/CocktailDataBaseSO", order = 0)]
    public class CocktailRecipeDatabaseSO : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<CocktailRecipeSO> itemListForSerialize = new();

        public HashSet<CocktailRecipeSO> recipes = new();

        public IReadOnlyCollection<CocktailRecipeSO> Recipes => recipes;

        public void AddCockTail(CocktailRecipeSO cocktailRecipeSo)
        {
            if (cocktailRecipeSo == null)
                return;

            recipes.Add(cocktailRecipeSo);
        }

        public void OnBeforeSerialize()
        {
            itemListForSerialize.Clear();

            foreach (var item in recipes)
            {
                itemListForSerialize.Add(item);
            }
        }

        public void OnAfterDeserialize()
        {
            recipes.Clear();

            foreach (var item in itemListForSerialize)
            {
                if (item != null)
                    recipes.Add(item);
            }
        }

        public void Reset()
        {
            recipes.Clear();
            itemListForSerialize.Clear();
        }
    }
}