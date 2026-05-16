namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    // 셰이커에 재료를 추가했을 때의 처리 결과
    public enum AddAlcoholResult
    {
        Added,            // 정상 추가됨
        AddedAfterFail,   // 이미 실패 상태이지만 재료는 추가됨
        AlreadyContained, // 이미 셰이커에 들어있는 재료
        WrongOrder,       // 재료는 맞지만 투입 순서가 틀림
        WrongIngredient,  // 레시피에 없는 재료
        Completed,        // 마지막 재료 투입으로 레시피 완성
        FullAfterFail,    // 재료가 모두 채워졌으나 실패 상태
        Full              // 이미 셰이커가 가득 참 (추가 불가)
    }
}