using System;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story
{
    /// <summary>
    /// 스토리 시스템에서 사용되는 로그 항목을 나타내는 클래스입니다.
    /// 각 로그 항목은 대사, 선택지, 이벤트 등 스토리 진행 중 발생한 하나의 기록을 나타냅니다.
    /// 로그 항목의 유형, 관련된 에피소드 및 대사 ID, 발화자 정보, 표시할 텍스트, 음성 참조,
    /// 그리고 로그 내에서의 순서 등을 포함합니다.
    /// </summary
    
    [Serializable]
    public sealed class StoryLogEntry
    {
        // 로그 항목의 유형을 나타내는 열거형입니다.
        // 대사, 내레이션, 선택지 프롬프트, 선택지 결과, 시스템 메시지 등 다양한 유형이 있을 수 있습니다.
        public StoryLogEntryType entryType;
        // 이 로그 항목과 관련된 에피소드 ID입니다.
        // 스토리 진행 중 어떤 에피소드에서 발생한 로그인지 식별하는 데 사용됩니다.
        public string episodeId;
        // 이 로그 항목과 관련된 대사 ID입니다.
        // 대사 로그 항목인 경우, 어떤 대사에서 발생한 로그인지 식별하는 데 사용됩니다.
        public string lineId;

        // 발화자 정보입니다. 대사 로그 항목인 경우, 이 필드에 발화자의 캐릭터 정의가 참조됩니다.
        public CharacterDefinitionSO speaker;
        // 로그 항목에 표시할 텍스트입니다. 대사 로그 항목인 경우, 이 필드에 대사 텍스트가 저장됩니다.
        public string displayName;
        [TextArea] public string text;

        // 이 로그 항목에 음성 정보가 포함될 때 사용하는 참조 클래스입니다.
        public StoryVoiceRef voice;
        // 로그 내에서 이 항목의 순서를 나타내는 정수입니다.
        // 로그 항목이 추가될 때마다 증가하는 값으로,
        // 로그 내에서 항목들이 어떤 순서로 발생했는지 식별하는 데 사용됩니다.
        public int sequence;
    }
}