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

        public bool visible = true;

        [Tooltip("Stage local world position offset added on top of parallaxBase. (0,0)=parallaxBase center.")]
        public Vector2 stageLocalPosition = Vector2.zero;

        [Tooltip("Uniform scale multiplier. finalScale = BackgroundDefinitionSO.BaseScaleMultiplier * scaleMultiplier.")]
        public float scaleMultiplier = 1f;

        public Color tint = Color.white;

        public int sortOrder = -100;

        public string ResolvedBackgroundKey =>
            !string.IsNullOrWhiteSpace(backgroundKey)
                ? backgroundKey
                : background != null
                    ? background.BackgroundId
                    : string.Empty;

        public bool HasBackground =>
            background != null || !string.IsNullOrWhiteSpace(backgroundKey) || !visible;

        public Color EffectiveTint =>
            background != null
                ? background.DefaultTint * tint
                : tint;

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
