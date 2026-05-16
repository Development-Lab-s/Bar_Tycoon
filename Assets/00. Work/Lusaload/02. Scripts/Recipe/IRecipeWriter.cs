using _00._Work.Lusaload._02._Scripts.SO;

namespace _00._Work.Lusaload._02._Scripts.Recipe
{
    // 활성 레시피를 변경하거나 초기화하는 인터페이스
    public interface IRecipeWriter
    {
        void SetRecipe(CocktailRecipeSO recipeSO); // 새 레시피로 교체하고 시퀀스를 재생성
        void ClearRecipe();                         // 레시피와 시퀀스를 null로 초기화
    }
}