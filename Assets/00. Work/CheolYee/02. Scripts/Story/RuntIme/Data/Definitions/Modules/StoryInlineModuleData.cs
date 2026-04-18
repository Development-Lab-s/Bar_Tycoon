using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    // ──────────────────────────────────────────────────────────────────────────
    //  베이스 — 필드 레이아웃 불변 (SerializeReference 타입명 변경 금지)
    // ──────────────────────────────────────────────────────────────────────────
    [Serializable]
    public abstract class StoryInlineModuleData
    {
        [SerializeField] private StoryModuleTiming timing = StoryModuleTiming.BeforeDialogue;
        [SerializeField] private bool isBlocking;
        [SerializeField] private bool canSkip = true;
        [SerializeField] private bool affectsAutoAdvance;

        public StoryModuleTiming Timing          => timing;
        public bool              IsBlocking       => isBlocking;
        public bool              CanSkip          => canSkip;
        public bool              AffectsAutoAdvance => affectsAutoAdvance;

        public virtual string DisplayName => GetType().Name;

        protected StoryInlineModuleData() { }

        // migration 전용 — 기존 SO 값을 그대로 복사할 때 사용
        protected StoryInlineModuleData(
            StoryModuleTiming timing,
            bool isBlocking,
            bool canSkip,
            bool affectsAutoAdvance)
        {
            this.timing             = timing;
            this.isBlocking         = isBlocking;
            this.canSkip            = canSkip;
            this.affectsAutoAdvance = affectsAutoAdvance;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  WaitInlineModuleData — 필드 레이아웃 불변
    // ──────────────────────────────────────────────────────────────────────────
    [StoryModuleMetadata("Wait", category: "Timing", accentColorHex: "#5B9BD5", sortPriority: 10)]
    [Serializable]
    public sealed class WaitInlineModuleData : StoryInlineModuleData
    {
        [SerializeField, Min(0f)] private float duration = 0.4f;
        [SerializeField] private bool useUnscaledTime = true;

        public float Duration        => duration;
        public bool  UseUnscaledTime => useUnscaledTime;

        public override string DisplayName => "Wait";

        public WaitInlineModuleData() { }

        internal WaitInlineModuleData(
            StoryModuleTiming timing, bool isBlocking, bool canSkip, bool affectsAutoAdvance,
            float duration, bool useUnscaledTime)
            : base(timing, isBlocking, canSkip, affectsAutoAdvance)
        {
            this.duration        = duration;
            this.useUnscaledTime = useUnscaledTime;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  CharacterClearInlineModuleData — 필드 레이아웃 불변
    // ──────────────────────────────────────────────────────────────────────────
    [StoryModuleMetadata("Character Clear", category: "Character", accentColorHex: "#70AD47", sortPriority: 20)]
    [Serializable]
    public sealed class CharacterClearInlineModuleData : StoryInlineModuleData
    {
        public override string DisplayName => "Character Clear";

        public CharacterClearInlineModuleData() { }

        internal CharacterClearInlineModuleData(
            StoryModuleTiming timing, bool isBlocking, bool canSkip, bool affectsAutoAdvance)
            : base(timing, isBlocking, canSkip, affectsAutoAdvance) { }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ChoiceInlineModuleData
    //  — 필드 레이아웃 불변
    //  — IStoryChoiceLikeModule / IStoryGraphConnectableModule 구현 추가
    //    (Phase 2 런타임 통일을 위한 어댑터 역할, Phase 5에서 제거 예정)
    // ──────────────────────────────────────────────────────────────────────────
    [StoryModuleMetadata("Choice", category: "Flow", accentColorHex: "#FF8C00", sortPriority: 100)]
    [Serializable]
    public sealed class ChoiceInlineModuleData : StoryInlineModuleData,
        IStoryChoiceLikeModule,
        IStoryGraphConnectableModule
    {
        [Serializable]
        public sealed class ChoiceOption : IStoryChoiceOption
        {
            public string optionId;
            [TextArea] public string text;
            public string reactionStartLineId;
            public string convergeLineId;

            // IStoryChoiceOption — 명시적 구현으로 기존 필드명 유지
            string IStoryChoiceOption.OptionId            => optionId;
            string IStoryChoiceOption.Text                => text;
            string IStoryChoiceOption.ReactionStartLineId => reactionStartLineId;
            string IStoryChoiceOption.ConvergeLineId      => convergeLineId;
        }

        [SerializeField] private string choiceId;
        [SerializeField] private List<ChoiceOption> options = new();

        public string ChoiceId => choiceId;

        /// <summary>concrete 타입 접근이 필요한 에디터 코드는 이 프로퍼티를 사용합니다.</summary>
        public IReadOnlyList<ChoiceOption> Options => options;

        public override string DisplayName => "Choice";

        public ChoiceInlineModuleData() { }

        internal ChoiceInlineModuleData(
            StoryModuleTiming timing, bool isBlocking, bool canSkip, bool affectsAutoAdvance,
            string choiceId, List<ChoiceOption> options)
            : base(timing, isBlocking, canSkip, affectsAutoAdvance)
        {
            this.choiceId = choiceId;
            this.options  = options ?? new();
        }

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
