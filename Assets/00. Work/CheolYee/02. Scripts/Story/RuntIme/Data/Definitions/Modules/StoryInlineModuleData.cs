using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    /// <summary>
    /// SO 자산 없이 StoryLineSO 안에 직접 직렬화되는 인라인 모듈 베이스입니다.
    /// [SerializeReference]와 함께 사용합니다.
    /// </summary>
    [Serializable]
    public abstract class StoryInlineModuleData
    {
        [SerializeField] private StoryModuleTiming timing = StoryModuleTiming.BeforeDialogue;
        [SerializeField] private bool isBlocking = false;
        [SerializeField] private bool canSkip = true;
        [SerializeField] private bool affectsAutoAdvance = false;

        public StoryModuleTiming Timing => timing;
        public bool IsBlocking => isBlocking;
        public bool CanSkip => canSkip;
        public bool AffectsAutoAdvance => affectsAutoAdvance;

        public virtual string DisplayName => GetType().Name;
    }

    [Serializable]
    public sealed class WaitInlineModuleData : StoryInlineModuleData
    {
        [SerializeField, Min(0f)] private float duration = 0.4f;
        [SerializeField] private bool useUnscaledTime = true;

        public float Duration => duration;
        public bool UseUnscaledTime => useUnscaledTime;

        public override string DisplayName => "Wait";
    }

    [Serializable]
    public sealed class CharacterClearInlineModuleData : StoryInlineModuleData
    {
        public override string DisplayName => "Character Clear";
    }
}
