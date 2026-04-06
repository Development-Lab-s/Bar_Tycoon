using System.Collections.Generic;

namespace _00._Work.CheolYee._02._Scripts.Story
{
    /// <summary>
    /// 스토리 시스템에서 사용되는 세션 데이터를 나타내는 클래스입니다.
    /// 각 세션은 현재 진행 중인 에피소드, 현재 대사 ID, 스토리 진행 방식,
    /// UI 표시 여부, 로그 기록, 선택지 결과, 활성 배우 정보 등을 포함합니다.
    /// 세션 데이터는 스토리 진행 중에 변경되며, 스토리 시스템이 플레이어의 진행 상황을 관리하는 데 사용됩니다.
    /// </summary>
    
    public sealed class StorySession
    {
        // 현재 진행 중인 에피소드입니다. 스토리 시스템이 플레이어에게 보여주고 있는 에피소드를 참조합니다.
        public StoryEpisodeSO Episode { get; private set; }
        // 현재 진행 중인 대사 ID입니다. 스토리 시스템이 플레이어에게 보여주고 있는 대사의 ID를 나타냅니다.
        public string CurrentLineId { get; private set; }

        // 스토리 진행 방식을 나타내는 열거형입니다.
        // 수동 진행, 자동 진행, 빠르게 진행 등의 다양한 방식이 있을 수 있습니다.
        public StoryAdvanceMode AdvanceMode { get; set; } = StoryAdvanceMode.Manual;
        // UI 표시 여부를 나타내는 불리언입니다. true인 경우 스토리 UI가 숨겨지고, false인 경우 표시됩니다.
        public bool IsUiHidden { get; set; }
        // 플레이어가 스토리를 일시 중지하고 나중에 다시 시작할 때, 어디서부터 재개할지 표시하는 플래그입니다.
        public bool HasPendingResumePoint { get; set; }

        // 스토리 진행 중 발생한 로그 항목들의 리스트입니다.
        // 대사, 선택지, 이벤트 등 스토리 진행 중 발생한 기록들이 저장됩니다.
        public readonly List<StoryLogEntry> Logs = new();
        // 플레이어가 선택한 선택지의 결과를 저장하는 딕셔너리입니다.
        // 선택지 ID를 키로, 선택한 옵션 ID를 값으로 저장합니다.
        public readonly Dictionary<string, string> ChoiceResults = new();
        // 현재 스토리에 등장하는 활성 배우들의 정보를 저장하는 딕셔너리입니다.
        // 배우 인스턴스 ID를 키로, 해당 배우의 런타임 핸들을 값으로 저장합니다.
        public readonly Dictionary<string, ActorRuntimeHandle> ActiveActors = new();

        // 세션을 초기화하는 메서드입니다. 새로운 에피소드를 시작할 때 호출됩니다.
        public void Begin(StoryEpisodeSO episode)
        {
            Episode = episode;
            CurrentLineId = episode != null ? episode.EntryLineId : string.Empty;

            AdvanceMode = StoryAdvanceMode.Manual;
            IsUiHidden = false;
            HasPendingResumePoint = false;

            Logs.Clear();
            ChoiceResults.Clear();
            ActiveActors.Clear();
        }
        
        // 현재 대사에서 다음 대사로 이동할 때 호출되는 메서드입니다.
        // 다음 대사의 ID를 인자로 받아 CurrentLineId를 업데이트합니다.
        public void MoveTo(string nextLineId) 
        {
            CurrentLineId = nextLineId;
        }

        // 플레이어가 스토리를 일시 중지하고 나중에 다시 시작할 때, 어디서부터 재개할지 표시하는 메서드입니다.
        public void MarkPendingResume() 
        {
            HasPendingResumePoint = true;
        }
    }
}