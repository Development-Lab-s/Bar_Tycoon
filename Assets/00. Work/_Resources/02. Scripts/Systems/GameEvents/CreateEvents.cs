using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems.GameEvents
{
    public static class CreateEvents
    {
        public static readonly CreateEffectEvent CreateEffect = new CreateEffectEvent();
    }

    public class CreateEffectEvent : GameEvent
    {
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public int ClipHash { get; private set; }
        public float Duration { get; private set; }
        public bool IsPoolingEffect { get; private set; }

        public CreateEffectEvent Init(Vector3 position, Quaternion rotation, int clipHash, float duration = 1f, bool isPoolingEffect = false)
        {
            Position = position;
            Rotation = rotation;
            ClipHash = clipHash;
            Duration = duration;
            IsPoolingEffect = isPoolingEffect;
            return this;
        }
    }
}