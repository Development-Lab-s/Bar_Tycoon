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
        [SerializeField] private float holdDuration = 0f;

        // Direction-change tracking for auto-setting holdDuration defaults in the editor.
        [SerializeField, HideInInspector] private bool _holdInitialized;
        [SerializeField, HideInInspector] private StoryFadeDirection _prevDirection;

        public StoryFadeDirection Direction => direction;
        public float HoldDuration => holdDuration;
        public override string DisplayName => "Fade";

#if UNITY_EDITOR
        private void OnValidate()
        {
            var so = new SerializedObject(this);
            SerializedProperty dirProp        = so.FindProperty("direction");
            SerializedProperty prevDirProp    = so.FindProperty("_prevDirection");
            SerializedProperty holdInitProp   = so.FindProperty("_holdInitialized");
            SerializedProperty holdProp       = so.FindProperty("holdDuration");
            SerializedProperty timingProp     = so.FindProperty("timing");
            SerializedProperty blockingProp   = so.FindProperty("isBlocking");
            SerializedProperty canSkipProp    = so.FindProperty("canSkip");
            SerializedProperty autoAdvanceProp = so.FindProperty("affectsAutoAdvance");

            bool isFadeIn = dirProp != null && dirProp.enumValueIndex == (int)StoryFadeDirection.FadeIn;

            // First-time initialization: set holdDuration default based on direction.
            if (holdInitProp != null && !holdInitProp.boolValue)
            {
                if (holdProp != null)
                    holdProp.floatValue = isFadeIn ? 0.5f : 0f;
                if (prevDirProp != null && dirProp != null)
                    prevDirProp.enumValueIndex = dirProp.enumValueIndex;
                holdInitProp.boolValue = true;
            }
            // Auto-update holdDuration when direction changes.
            else if (dirProp != null && prevDirProp != null &&
                     dirProp.enumValueIndex != prevDirProp.enumValueIndex)
            {
                if (holdProp != null)
                    holdProp.floatValue = isFadeIn ? 0.5f : 0f;
                prevDirProp.enumValueIndex = dirProp.enumValueIndex;
            }

            if (holdProp != null)
                holdProp.floatValue = Mathf.Max(0f, holdProp.floatValue);

            // FadeIn  → BeforeDialogue or AfterDialogue (not WithDialogue)
            // FadeOut → WithDialogue only (runs in parallel with stage/text/sound)
            if (timingProp != null)
            {
                if (isFadeIn)
                {
                    if (timingProp.enumValueIndex == (int)StoryModuleTiming.WithDialogue)
                        timingProp.enumValueIndex = (int)StoryModuleTiming.BeforeDialogue;
                }
                else
                {
                    timingProp.enumValueIndex = (int)StoryModuleTiming.WithDialogue;
                }
            }

            // FadeIn is blocking; FadeOut advance-blocking is handled by withModulesTask in StoryRunner.
            if (blockingProp != null)    blockingProp.boolValue    = isFadeIn;
            if (canSkipProp != null)     canSkipProp.boolValue     = false;
            if (autoAdvanceProp != null) autoAdvanceProp.boolValue = isFadeIn;

            so.ApplyModifiedPropertiesWithoutUndo();
        }
#endif
    }
}
