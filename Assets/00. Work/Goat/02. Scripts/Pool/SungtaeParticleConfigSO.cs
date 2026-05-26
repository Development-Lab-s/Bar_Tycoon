using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Pool
{
    [CreateAssetMenu(fileName = "SungtaeParticleConfig", menuName = "BBJ/SungtaeParticleConfig", order = 0)]
    public class SungtaeParticleConfigSO : ScriptableObject
    {
        public List<SungtaeParticleTypeConfig> sungtaeTypes = new();
        
        public EventChannelSO particleChannel;
        public PoolManagerSo poolManager;
        public PoolItemSo particlePoolItem;
        
    }
    
    [Serializable]
    public class SungtaeParticleTypeConfig
    {
        public Material material;
    }
}