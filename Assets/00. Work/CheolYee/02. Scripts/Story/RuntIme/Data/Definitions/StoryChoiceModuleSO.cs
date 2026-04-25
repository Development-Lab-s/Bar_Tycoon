using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    [StoryModuleMetadata("Choice", category: "Flow", accentColorHex: "#FF8C00", sortPriority: 100)]
    [CreateAssetMenu(fileName = "ChoiceModule", menuName = "Story/Modules/Choice")]
    public sealed class StoryChoiceModuleSO : StoryModuleSO, IStoryChoiceLikeModule, IStoryGraphConnectableModule
    {
        [Serializable]
        public sealed class ChoiceOption : IStoryChoiceOption
        {
            public string optionId;
            [TextArea] public string text;
            public string reactionStartLineId;

            // IStoryChoiceOption — 명시적 구현으로 기존 필드명 유지
            string IStoryChoiceOption.OptionId            => optionId;
            string IStoryChoiceOption.Text                => text;
            string IStoryChoiceOption.ReactionStartLineId => reactionStartLineId;
        }

        [SerializeField] private string choiceId;
        [SerializeField] private List<ChoiceOption> options = new();

        public string ChoiceId => choiceId;

        /// <summary>concrete 타입 접근이 필요한 에디터 코드는 이 프로퍼티를 사용합니다.</summary>
        public IReadOnlyList<ChoiceOption> Options => options;

        // ── IStoryChoiceLikeModule ──────────────────────────────────────────
        IReadOnlyList<IStoryChoiceOption> IStoryChoiceLikeModule.Options => options;

        // ── IStoryGraphConnectableModule ────────────────────────────────────
        public IReadOnlyList<StoryModulePortDescriptor> GetPorts()
        {
            var ports = new StoryModulePortDescriptor[options.Count];
            for (int i = 0; i < options.Count; i++)
            {
                string label = string.IsNullOrEmpty(options[i].text) ? $"Option {i}" : options[i].text;
                ports[i] = new StoryModulePortDescriptor(i, label);
            }
            return ports;
        }

        public string GetPortConnection(int portIndex)
        {
            if ((uint)portIndex >= (uint)options.Count) return null;
            return options[portIndex].reactionStartLineId;
        }

        /// <remarks>에디터에서 호출 시 반드시 SerializedObject.ApplyModifiedProperties() 로 감싸야 Undo가 동작합니다.</remarks>
        public void SetPortConnection(int portIndex, string targetLineId)
        {
            if ((uint)portIndex >= (uint)options.Count) return;
            options[portIndex].reactionStartLineId = targetLineId;
        }
    }
}
