using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    /// 스토리 시스템의 핵심 기능을 정의하는 인터페이스입니다.
    public interface IStoryService
    {
        // 현재 스토리가 재생 중인지 여부를 나타내는 불리언입니다.
        // 스토리가 재생 중이면 true, 그렇지 않으면 false입니다.
        bool IsStoryOpen { get; }
        // 현재 활성화된 에피소드의 ID입니다. 스토리가 재생 중일 때,
        // 어떤 에피소드가 활성화되어 있는지 식별하는 데 사용됩니다. 스토리가 열려 있지 않으면 빈 문자열입니다.
        string ActiveEpisodeId { get; }

        // 유니테스크 기반으로 작동하는 스토리 재생 메서드 입니다. 하위 개체에서 구현합니다.
        UniTask<StoryPlayResult> PlayAsync(StoryPlayRequest request, CancellationToken ct = default);
        // 유니테스크 기반으로 작동하는 스토리 재개 메서드입니다. 하위 개체에서 구현합니다.
        UniTask<StoryPlayResult> ResumeAsync(string episodeId, CancellationToken ct = default);
        // 유니테스크 기반으로 작동하는 스토리 닫기(종료) 메서드입니다. 하위 개체에서 구현합니다.
        UniTask CloseActiveStoryAsync(
            StoryCloseReason reason,
            bool saveResumePoint,
            CancellationToken ct = default);
        
        // 다시 들어온 경우 재개할 곳이 있다면 얻어오는 메서드입니다. 하위 개체에서 구현합니다.
        bool TryGetResumePoint(string episodeId, out StoryResumePoint resumePoint);
    }
}