using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared
{
    /// <summary>
    /// Shared transform calculator for story stage actors.
    /// Used by both StoryPreviewWindow (editor) and runtime to ensure position/scale parity.
    ///
    /// Coordinate system: stageLocalPosition is StageRoot-relative world units.
    ///   (0,0) = StageRoot center. (2,0) = 2 world units right of StageRoot. Unclamped.
    ///
    /// Scale: finalUniformScale = CharacterDefinitionSO.BaseScaleMultiplier * StoryActorStateData.scaleMultiplier.
    ///   No PPU correction. No pivot offset. No clamp.
    /// </summary>
    public static class StoryActorStageTransformCalculator
    {
        /// <summary>
        /// Converts stageLocalPosition (stage local world units) to world position.
        /// stageRootCenter is typically the StageRoot transform world position (often Vector3.zero).
        /// </summary>
        public static Vector3 WorldPosition(
            Vector2 stageLocalPosition,
            Vector3 stageRootCenter,
            float z = 0f)
        {
            return new Vector3(
                stageRootCenter.x + stageLocalPosition.x,
                stageRootCenter.y + stageLocalPosition.y,
                z);
        }

        /// <summary>
        /// Returns uniform localScale for the actor:
        ///   finalScale = CharacterDefinitionSO.BaseScaleMultiplier * StoryActorStateData.scaleMultiplier
        /// No sprite PPU correction. No pivot offset.
        /// </summary>
        public static Vector3 UniformWorldScale(StoryActorStateData state)
        {
            if (state == null)
                return Vector3.one;

            float baseScale = state.actor != null ? state.actor.BaseScaleMultiplier : 1f;
            float lineScale = state.scaleMultiplier > 0f ? state.scaleMultiplier : 1f;
            float uniform = baseScale * lineScale;
            return new Vector3(uniform, uniform, 1f);
        }
    }
}
