namespace BBJ.Order
{
    public enum OrderWorkPhase
    {
        ReadyForServer  = 0, // 주문 대기 — 서버(또는 플레이어)가 손님에게 주문받으러 감
        PendingCook     = 1, // 요리 대기 — 요리사가 요리 (티켓 초기 단계)
        ReadyForServe   = 2, // 서빙 대기 — 서버가 음식 서빙
        Eating          = 3, // 식사 중   — 손님이 식사 (EatWorkSO 완료 후 다음 단계로)
        ReadyForCashier = 4, // 계산 대기 — 캐셔가 결제 처리
        Done            = 5,
    }
}
