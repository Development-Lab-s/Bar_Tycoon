using System;
using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    /// <summary>
    /// 스토리 시스템에서 사용되는 선택지 모듈의 정의을 담는 ScriptableObject 클래스입니다.
    /// 선택지 ID, 프롬프트 텍스트, 각 선택지 옵션의 텍스트와 그에 따른 반응 및 수렴 대사 ID 등을 포함합니다.
    /// </summary>
    
    [CreateAssetMenu(fileName = "ChoiceModule", menuName = "Story/Modules/Choice")]
    public sealed class StoryChoiceModuleSO : StoryModuleSO
    {
        /// 선택지 옵션을 나타내는 내부 클래스입니다.
        [Serializable]
        public sealed class ChoiceOption
        {
            // 각 선택지 옵션의 ID입니다.
            // 스토리 진행 로직에서 이 ID를 참조하여 어떤 선택이 이루어졌는지 식별할 수 있습니다.
            public string optionId;
            // 선택지 옵션에 표시될 텍스트입니다.
            [TextArea] public string text;
            // 이 선택지 옵션이 선택되었을 때 반응할 대사 ID입니다.
            public string reactionStartLineId;
            // 이 선택지 옵션이 선택되었을 때 수렴될 대사 ID입니다.
            public string convergeLineId;
        }

        // 선택지 모듈의 ID입니다. 스토리 진행 로직에서 이 ID를 참조하여 어떤 선택지가 등장하는지 식별할 수 있습니다.
        [SerializeField] private string choiceId;
        // 선택지 프롬프트 텍스트입니다. 플레이어에게 선택지를 제시할 때 이 텍스트가 함께 표시됩니다.
        [TextArea] [SerializeField] private string prompt;
        // 선택지 옵션들의 리스트입니다. 각 옵션은 플레이어가 선택할 수 있는 하나의 선택지를 나타냅니다.
        [SerializeField] private List<ChoiceOption> options = new();

        // 각 필드에 대한 공개 읽기 전용 프로퍼티입니다.
        public string ChoiceId => choiceId;
        public string Prompt => prompt;
        public IReadOnlyList<ChoiceOption> Options => options;
    }
}