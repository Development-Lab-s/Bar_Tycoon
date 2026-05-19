using System;

namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    // 현재 칵테일 제조 시퀀스를 읽고 변경 알림을 받는 인터페이스
    public interface ISequenceReader
    {
        CocktailOrderSequence CurrentSequence { get; }                  // 현재 활성 시퀀스. 없으면 null
        event Action<CocktailOrderSequence> OnSequenceChanged;          // 시퀀스가 교체될 때 발행 (null이면 초기화)
    }
}