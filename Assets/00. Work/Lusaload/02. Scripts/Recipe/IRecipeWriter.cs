using _00._Work.Lusaload._02._Scripts.SO;

namespace _00._Work.Lusaload._02._Scripts.Recipe
{
    public interface IRecipeWriter
    {
        void SetRecipe(CocktailRecipeSO recipeSO);
        void ClearRecipe();
    }
}