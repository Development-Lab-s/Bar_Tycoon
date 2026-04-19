using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts;
using Cysharp.Threading.Tasks;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    /// 선택지 패널 컨트롤러 인터페이스입니다. 선택지 표시와 관련된 모든 기능을 정의합니다.
    public interface IChoicePanelController
    {
        bool IsRevealing { get; }
        bool IsChoiceOpen { get; }

        UniTask<StoryChoiceResult> ShowChoicesAsync(StoryChoiceModuleSO module, CancellationToken ct);
        void CompleteReveal();
        void CloseImmediate();
    }
}