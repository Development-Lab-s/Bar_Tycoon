using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    /// <summary>
    /// Absolute actor state at a story line. StoryStageLayoutModuleSO owns these
    /// values as sub-asset-authored source data through StoryLineSO.modules.
    /// </summary>
    [Serializable]
    public sealed class StoryActorStateData
    {
        [Tooltip("Character definition used for preview visuals and runtime prefab lookup.")]
        public CharacterDefinitionSO actor;

        [Tooltip("Stable actor key. Falls back to CharacterDefinitionSO.CharacterId when empty.")]
        public string actorKey = "";

        [Tooltip("Unique stage actor instance key. Multiple entries may reference the same CharacterDefinitionSO.")]
        public string actorInstanceKey = "";

        [Tooltip("Normalized stage position. X: 0=left, 1=right / Y: 0=bottom, 1=top.")]
        public Vector2 normalizedPosition = new Vector2(0.5f, 0f);

        [Tooltip("Per-line scale multiplier. Default character scale is applied before this value.")]
        public Vector2 scale = Vector2.one;

        [Tooltip("Legacy horizontal scale/flip multiplier. Kept for existing preview authoring data.")]
        public float scaleX = 1f;

        [Tooltip("Whether this actor is visible at this line state.")]
        public bool visible = true;

        [Tooltip("Whether this actor is focused. Unfocused actors can be dimmed by preview/runtime.")]
        public bool focused = true;

        [NonSerialized]
        public float focusVisualAlpha = -1f;

        [Tooltip("Stage sorting priority. Larger values are rendered in front.")]
        public int sortOrder = 0;

        [Tooltip("Pose key for sprite/animation lookup.")]
        public string poseKey = "";

        [Tooltip("Expression enum for full-body sprite lookup.")]
        public StoryExpressionType expression = StoryExpressionType.Neutral;

        [HideInInspector]
        public string expressionKey = "";

        [Tooltip("Optional motion profile key for shared presets in later authoring steps.")]
        public string motionProfileKey = "";

        [Tooltip("Transition used when this actor enters the accumulated stage state.")]
        public StoryEnterMotionType enterMotion = StoryEnterMotionType.FadeIn;

        [Range(0f, 3f)]
        public float enterDuration = 0.35f;

        [Tooltip("Transition used when this actor moves between line states.")]
        public StoryStageMoveMotionType moveMotion = StoryStageMoveMotionType.Linear;

        [Range(0f, 3f)]
        public float moveDuration = 0.2f;

        [Tooltip("Transition used when this actor exits the accumulated stage state.")]
        public StoryEnterMotionType exitMotion = StoryEnterMotionType.FadeIn;

        [Range(0f, 3f)]
        public float exitDuration = 0.25f;

        public string ResolvedActorKey =>
            !string.IsNullOrWhiteSpace(actorInstanceKey)
                ? actorInstanceKey
                : ResolvedCharacterKey;

        public string ResolvedCharacterKey =>
            !string.IsNullOrWhiteSpace(actorKey)
                ? actorKey
                : ResolveActorKey(actor);

        public Vector2 EffectiveScale
        {
            get
            {
                Vector2 baseScale = ResolveNonZeroScale(actor != null ? actor.DefaultStageScale : Vector2.one);
                Vector2 lineScale = ResolveNonZeroScale(scale);
                return new Vector2(baseScale.x * lineScale.x * scaleX, baseScale.y * lineScale.y);
            }
        }

        public Vector2 EffectiveOffset =>
            actor != null ? actor.DefaultStageOffset : Vector2.zero;

        public Vector2 EffectivePivot =>
            actor != null ? actor.DefaultStagePivot : new Vector2(0.5f, 0f);

        public StoryExpressionType ResolvedExpression =>
            actor != null
            && expression == StoryExpressionType.Neutral
            && string.IsNullOrWhiteSpace(expressionKey)
                ? actor.DefaultExpression
                : expression;

        public float EffectiveFocusAlpha =>
            focusVisualAlpha >= 0f ? Mathf.Clamp01(focusVisualAlpha) : focused ? 1f : 0.65f;

        public void SyncActorKey()
        {
            if (string.IsNullOrWhiteSpace(actorKey))
                actorKey = ResolveActorKey(actor);
        }

        public void EnsureActorInstanceKey(string fallbackKey)
        {
            if (!string.IsNullOrWhiteSpace(actorInstanceKey))
                return;

            actorInstanceKey = !string.IsNullOrWhiteSpace(fallbackKey)
                ? fallbackKey
                : ResolvedCharacterKey;
        }

        public bool MatchesActor(CharacterDefinitionSO target)
        {
            if (target == null)
                return false;

            if (actor == target)
                return true;

            string targetKey = ResolveActorKey(target);
            return !string.IsNullOrWhiteSpace(targetKey) && ResolvedCharacterKey == targetKey;
        }

        public bool MatchesActorInstance(string instanceKey) =>
            !string.IsNullOrWhiteSpace(instanceKey) && ResolvedActorKey == instanceKey;

        public static string ResolveActorKey(CharacterDefinitionSO actorDefinition) =>
            actorDefinition != null
                ? !string.IsNullOrWhiteSpace(actorDefinition.CharacterId)
                    ? actorDefinition.CharacterId
                    : actorDefinition.name
                : string.Empty;

        private static Vector2 ResolveNonZeroScale(Vector2 value) =>
            Mathf.Approximately(value.x, 0f) && Mathf.Approximately(value.y, 0f)
                ? Vector2.one
                : value;

        public StoryActorStateData ShallowClone() => (StoryActorStateData)MemberwiseClone();
    }
}
