using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts;
using Cysharp.Threading.Tasks;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    /// <summary>
    /// 선택지 패널 컨트롤러 인터페이스.
    /// IStoryChoiceLikeModule 을 통해 SO / 인라인 양쪽 Choice 모듈을 단일 경로로 처리합니다.
    /// </summary>
    public interface IChoicePanelController
    {
        bool IsRevealing  { get; }
        bool IsChoiceOpen { get; }

        UniTask<StoryChoiceResult> ShowChoicesAsync(IStoryChoiceLikeModule module, CancellationToken ct);
        void CompleteReveal();
        void CloseImmediate();
    }
}
