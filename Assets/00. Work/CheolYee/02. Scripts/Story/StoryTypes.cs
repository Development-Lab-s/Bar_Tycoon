using System;

namespace _00._Work.CheolYee._02._Scripts.Story
{
    public enum StoryModuleTiming //스토리 대사 타이밍
    {
        BeforeDialogue = 0,
        WithDialogue = 1,
        AfterDialogue = 2,
    }

    public enum StoryAdvanceMode //스토리 자동진행인지 수동진행인지
    {
        Manual = 0,
        Auto = 1,
    }

    public enum StoryLogEntryType //스토리 로그에 기록되는 항목의 유형
    {
        Dialogue = 0,
        Narration = 1,
        ChoicePrompt = 2,
        ChoiceResult = 3,
        System = 4,
    }

    public enum StoryFocusPolicy //스토리 진행 중 캐릭터 초점이 어떻게 변화할지
    {
        AutoFocusSpeaker = 0,
        KeepCurrent = 1,
        ClearFocus = 2,
    }

    public enum VoicePlaybackPolicy //스토리 대사에 음성이 있을 때 어떻게 재생할지
    {
        None = 0,
        AutoPlay = 1,
        ManualOnly = 2,
    }

    public enum StageAnchorType //스토리 진행 중 캐릭터가 화면에서 어느 위치에 배치될지
    {
        Left = 0,
        Center = 1,
        Right = 2,
    }

    [Serializable]
    public sealed class StoryVoiceRef //혹시 추가하고싶긴하지만 안될것같지만 아무튼 희망사항
    {
        // 스토리 대사에 음성 정보가 포함될 때 사용하는 참조 클래스
        public string voiceKey;
        public VoicePlaybackPolicy playbackPolicy = VoicePlaybackPolicy.None;
        public bool allowReplayInLog = true;
    }
}