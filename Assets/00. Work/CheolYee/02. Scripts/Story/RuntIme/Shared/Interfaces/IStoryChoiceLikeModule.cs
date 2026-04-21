using System.Collections.Generic;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    /// <summary>
    /// "선택지를 갖는 모듈"의 capability 인터페이스.
    /// SO 경로(StoryChoiceModuleSO)와 인라인 경로(ChoiceInlineModuleData) 모두 구현하여
    /// 런타임이 두 경로를 단일 경로로 처리할 수 있게 합니다.
    /// Phase 5에서 인라인 경로가 제거되면 SO 경로만 남습니다.
    /// </summary>
    public interface IStoryChoiceLikeModule
    {
        string ChoiceId { get; }
        IReadOnlyList<IStoryChoiceOption> Options { get; }
    }
}
