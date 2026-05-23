using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    public enum StoryFadeDirection
    {
        FadeIn,
        FadeOut,
    }

    [StoryModuleMetadata("Fade", category: "Stage", accentColorHex: "#2D89EF", sortPriority: 12)]
    public sealed class StoryFadeModuleSO : StoryModuleSO
    {
        [SerializeField] private StoryFadeDirection direction = StoryFadeDirection.FadeIn;

        public StoryFadeDirection Direction => direction;
        public override string DisplayName => "Fade";

#if UNITY_EDITOR
        private void OnValidate()
        {
            var so = new SerializedObject(this);
            SerializedProperty timingProp = so.FindProperty("timing");
            SerializedProperty blockingProp = so.FindProperty("isBlocking");
            SerializedProperty canSkipProp = so.FindProperty("canSkip");
            SerializedProperty autoAdvanceProp = so.FindProperty("affectsAutoAdvance");
            if (timingProp != null && timingProp.enumValueIndex == (int)StoryModuleTiming.WithDialogue)
                timingProp.enumValueIndex = (int)StoryModuleTiming.BeforeDialogue;
            if (blockingProp != null)
                blockingProp.boolValue = true;
            if (canSkipProp != null)
                canSkipProp.boolValue = false;
            if (autoAdvanceProp != null)
                autoAdvanceProp.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
#endif
    }
}
