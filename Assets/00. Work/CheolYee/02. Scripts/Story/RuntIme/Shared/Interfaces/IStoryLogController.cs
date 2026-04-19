using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.RuntimeModules;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    // 스토리 로그 컨트롤러 인터페이스입니다. 스토리 로그 기록과 관련된 모든 기능을 정의합니다.
    public interface IStoryLogController
    {
        void AppendLine(StoryLogEntry entry);
        void AppendChoiceResult(StoryLogEntry entry);
        void Open();
        void Close();
        void Clear();
    }
}