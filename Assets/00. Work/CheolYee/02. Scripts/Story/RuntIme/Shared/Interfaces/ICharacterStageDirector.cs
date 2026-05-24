using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    // 캐릭터 스테이지 디렉터 인터페이스입니다. 캐릭터 배치와 관련된 모든 기능을 정의합니다.
    public interface ICharacterStageDirector
    {
        UniTask EnsureSpeakerVisibleAsync(StoryLineSO line, CancellationToken ct);
        void ApplySpeakerFocus(StoryLineSO line);
        void ApplyBackgroundOnly(StoryStageLayoutModuleSO layout);
        void ApplyStageLayoutImmediate(StoryStageLayoutModuleSO layout);
        UniTask PrewarmLinePresentationAsync(StoryLineSO line, CancellationToken ct);
        Vector3 GetCurrentCameraCenter();
        void ClearAll();
    }
}
