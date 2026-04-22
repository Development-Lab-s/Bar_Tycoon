using System;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    public enum StoryActorKeyframeProperty
    {
        Position,
        Scale,
        Easing,
    }

    /// <summary>
    /// A single keyframe in a per-actor timeline track within one story line.
    /// normalizedTime 0 = line start, 1 = line end.
    /// Keyframes are evaluated relative to the actor's accumulated entry state from
    /// StoryActorStateData and do not replace the StoryLineSO.modules source of truth.
    /// </summary>
    [Serializable]
    public sealed class StoryActorKeyframeData
    {
        [Tooltip("Which property row this key belongs to.")]
        public StoryActorKeyframeProperty property = StoryActorKeyframeProperty.Position;

        [Tooltip("Position of this keyframe in the line: 0 = line start, 1 = line end.")]
        [Range(0f, 1f)]
        public float normalizedTime = 0f;

        [Tooltip("Absolute key time in seconds from the start of this line. Preferred by the timeline editor.")]
        [Min(0f)]
        public float timeSeconds = 0f;

        [Tooltip("Normalized stage position at this keyframe. X: 0=left, 1=right / Y: 0=bottom, 1=top.")]
        public Vector2 normalizedPosition;

        [Tooltip("Scale multiplier at this keyframe.")]
        public Vector2 scale = Vector2.one;

        [Tooltip("Legacy horizontal scale/flip at this keyframe.")]
        public float scaleX = 1f;

        [Tooltip("Legacy visibility value. Hidden in the MVP timeline UI.")]
        public bool visible = true;

        [Tooltip("Outgoing easing from this key to the next key in the same property row.")]
        public StoryStageMoveMotionType easing = StoryStageMoveMotionType.EaseInOut;

        public StoryActorKeyframeData ShallowClone() => (StoryActorKeyframeData)MemberwiseClone();
    }
}
