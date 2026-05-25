using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class SungtaeParticleEvent : GameEvent
    {
        public ParticleType type;
        public Vector3 position;

        public SungtaeParticleEvent Init(ParticleType type, Vector3 position)
        {
            this.type = type;
            this.position = position;
            return this;
        }
    }

    public enum ParticleType
    {
        HEART,
        DEFULT
    }
}