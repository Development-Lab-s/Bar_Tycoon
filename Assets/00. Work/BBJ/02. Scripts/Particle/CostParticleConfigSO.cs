using System.Collections.Generic;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace BBJ.Particle
{
    [CreateAssetMenu(fileName = "CostParticleConfig", menuName = "BBJ/CostParticleConfig", order = 0)]
    public class CostParticleConfigSO : ScriptableObject
    {
        public List<CostTypeConfig> costTypes = new();
        public EventChannelSO particleChannel;
        public PoolManagerSo poolManager;
        public PoolItemSo particlePoolItem;
    }
}
