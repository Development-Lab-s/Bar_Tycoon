namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    // ISequenceReader 의존성을 주입받는 컴포넌트가 구현하는 인터페이스
    public interface ISequenceReaderReceiver
    {
        // CocktailRecipeManager가 Awake에서 자동으로 호출해 reader를 주입
        void SetSequenceReader(ISequenceReader reader);
    }
}