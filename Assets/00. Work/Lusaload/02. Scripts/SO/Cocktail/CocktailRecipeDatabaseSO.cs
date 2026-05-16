using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    // 게임에 등록된 모든 칵테일 레시피 SO를 한 곳에서 관리하는 데이터베이스 SO
    [CreateAssetMenu(fileName = "CocktailDataBase", menuName = "Alcohol/CocktailDataBaseSO", order = 0)]
    public class CocktailRecipeDatabaseSO : ScriptableObject
    {
        public HashSet<CocktailRecipeSO> recipes = new(); // 등록된 칵테일 레시피 목록
    }
}