using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    /// 스토리 재개 지점의 저장과 관리를 담당하는 인터페이스입니다.
    public interface IStorySaveService
    {
        // 스토리 재개 지점을 저장할 때 사용하는 메서드입니다.
        // StorySession 객체를 인자로 받아 현재 세션의 상태를 기반으로 재개 지점을 저장합니다.
        bool TryGetResumePoint(string episodeId, out StoryResumePoint resumePoint);

        // 스토리 재개 지점을 저장하는 메서드입니다.
        // StorySession 객체를 인자로 받아 현재 세션의 상태를 기반으로 재개 지점을 저장합니다.
        void SaveResumePoint(StorySession session);

        // 특정 에피소드에 대한 재개 지점을 삭제하는 메서드입니다.
        // 예를 들어, 플레이어가 스토리를 완전히 완료했거나 재개 지점이 더 이상 유효하지 않은 경우에 호출될 수 있습니다.
        void ClearResumePoint(string episodeId);
    }
}