using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Particle
{
    public class CostParticleEvent : GameEvent
    {
        public CostParticleType type;
        public int amount;
        public Vector3 position;

        public CostParticleEvent Init(CostParticleType type, int amount, Vector3 position)
        {
            this.type = type;
            this.amount = amount;
            this.position = position;
            return this;
        }
    }
}
