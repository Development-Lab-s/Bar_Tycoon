using System.Collections.Generic;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using TMPro;
using UnityEngine;

namespace BBJ.Particle
{
    [CreateAssetMenu(fileName = "CostParticleConfig", menuName = "Goat/CostParticleConfig", order = 0)]
    public class CostParticleConfigSO : ScriptableObject
    {
        public List<CostTypeConfig> costTypes = new();
        public EventChannelSO particleChannel;
        public TMP_SpriteAsset spriteAsset;
        public PoolManagerSo poolManager;
        public PoolItemSo particlePoolItem;
        public int maxAtlasSize = 1024;
    }
}
