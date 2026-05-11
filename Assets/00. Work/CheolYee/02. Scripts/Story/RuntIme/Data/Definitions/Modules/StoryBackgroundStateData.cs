using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    [Serializable]
    public sealed class StoryBackgroundStateData
    {
        [Tooltip("Background definition used by preview and runtime.")]
        public BackgroundDefinitionSO background;

        [Tooltip("Stable background key. Falls back to BackgroundDefinitionSO.BackgroundId when empty.")]
        public string backgroundKey = "";

        [Tooltip("Optional variant key for a shared background definition.")]
        public string variantKey = "";

        public bool visible = true;
        public Vector2 normalizedOffset = Vector2.zero;
        public Vector2 scale = Vector2.one;
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        public Color tint = Color.white;

        [Range(0f, 1f)]
        public float opacity = 1f;

        public int sortOrder = -100;

        [Tooltip("Transition used when the background changes into this line state.")]
        public StoryEnterMotionType transitionMotion = StoryEnterMotionType.FadeIn;

        [Range(0f, 3f)]
        public float transitionDuration = 0.35f;

        public StoryEnterMotionType enterMotion = StoryEnterMotionType.FadeIn;

        [Range(0f, 3f)]
        public float enterDuration = 0.35f;

        public StoryEnterMotionType exitMotion = StoryEnterMotionType.FadeIn;

        [Range(0f, 3f)]
        public float exitDuration = 0.25f;

        public string ResolvedBackgroundKey =>
            !string.IsNullOrWhiteSpace(backgroundKey)
                ? backgroundKey
                : background != null
                    ? background.BackgroundId
                    : string.Empty;

        public bool HasBackground =>
            background != null || !string.IsNullOrWhiteSpace(backgroundKey) || !visible;

        public Vector2 EffectiveScale =>
            background != null
                ? new Vector2(background.DefaultScale.x * scale.x, background.DefaultScale.y * scale.y)
                : scale;

        public Vector2 EffectiveOffset =>
            background != null
                ? background.DefaultNormalizedOffset + normalizedOffset
                : normalizedOffset;

        public Vector2 EffectivePivot =>
            background != null
                ? background.DefaultPivot + (pivot - new Vector2(0.5f, 0.5f))
                : pivot;

        public Color EffectiveTint =>
            background != null
                ? background.DefaultTint * tint
                : tint;

        public float EffectiveOpacity =>
            background != null
                ? Mathf.Clamp01(background.DefaultOpacity * opacity)
                : Mathf.Clamp01(opacity);

        public int EffectiveSortOrder =>
            sortOrder != -100 || background == null
                ? sortOrder
                : background.DefaultSortOrder;

        public void SyncBackgroundKey()
        {
            if (string.IsNullOrWhiteSpace(backgroundKey) && background != null)
                backgroundKey = background.BackgroundId;
        }

        public StoryBackgroundStateData ShallowClone() =>
            (StoryBackgroundStateData)MemberwiseClone();
    }
}
