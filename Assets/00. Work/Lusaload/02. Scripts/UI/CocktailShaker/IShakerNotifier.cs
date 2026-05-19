using System;

namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    // 셰이커가 가득 찼을 때 외부에 알리는 이벤트 인터페이스
    public interface IShakerNotifier
    {
        event Action OnShakerFull; // 재료가 모두 채워졌을 때 발행
    }
}
