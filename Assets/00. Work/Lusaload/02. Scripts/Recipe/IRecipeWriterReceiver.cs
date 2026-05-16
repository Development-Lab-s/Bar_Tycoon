namespace _00._Work.Lusaload._02._Scripts.Recipe
{
    // IRecipeWriter 의존성을 주입받는 컴포넌트가 구현하는 인터페이스
    public interface IRecipeWriterReceiver
    {
        // CocktailRecipeManager가 Awake에서 자동으로 호출해 writer를 주입
        void SetRecipeWriter(IRecipeWriter writer);
    }
}