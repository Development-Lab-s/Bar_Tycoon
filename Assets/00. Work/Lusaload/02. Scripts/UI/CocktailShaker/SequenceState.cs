namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    // 칵테일 제조 시퀀스의 현재 진행 상태
    public enum SequenceState
    {
        InProgress, // 재료를 투입 중인 상태
        Completed,  // 올바른 순서로 모든 재료를 채워 완성
        Failed      // 틀린 재료나 순서 오류로 실패 확정
    }
}