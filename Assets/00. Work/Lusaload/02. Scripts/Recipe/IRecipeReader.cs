using _00._Work.Lusaload._02._Scripts.SO;

namespace _00._Work.Lusaload._02._Scripts.Recipe
{
    // 현재 활성 칵테일 레시피를 읽는 인터페이스
    public interface IRecipeReader
    {
        CocktailRecipeSO CurrentRecipe { get; } // 현재 선택된 레시피. 없으면 null
    }
}