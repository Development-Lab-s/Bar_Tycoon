using System.Collections.Generic;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using Cysharp.Threading.Tasks;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    /// <summary>
    /// 스테이지 레이아웃 모듈을 받아 캐릭터 enter / move / exit / focus 를 처리하는 디렉터 인터페이스.
    /// BasicStoryCharacterStageDirector 가 이를 구현하며,
    /// 디렉터 내부에서 현재 활성 액터 목록과 target 을 비교해 delta 처리한다.
    /// </summary>
    public interface IStoryStageDirector
    {
        /// <summary>
        /// target 상태를 적용한다. 디렉터가 현재 상태와 비교해 enter / move / exit 를 수행한다.
        /// </summary>
        UniTask ApplyStageStateAsync(IReadOnlyList<StoryActorStateData> targetActors, CancellationToken ct);

        void ClearAll();
    }
}
