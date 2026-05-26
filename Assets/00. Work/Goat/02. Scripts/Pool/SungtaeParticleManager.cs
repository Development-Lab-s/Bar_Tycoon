using System.Runtime.CompilerServices;
using _00._Work.Goat._02._Scripts.Events;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Pool
{
    public class SungtaeParticleManager : MonoBehaviour
    {
        [SerializeField] private SungtaeParticleConfigSO config;
        
        
        private void Start()
        {
            config.poolManager.InitializePool(transform);
            config.particleChannel.AddListener<SungtaeParticleEvent>(OnParticleEvent);
        }
        
        private void OnDisable()
        {
            config.particleChannel.RemoveListener<SungtaeParticleEvent>(OnParticleEvent);
        }

        private void OnParticleEvent(SungtaeParticleEvent evt)
        {
            int idx = (int)evt.type;

            if ((uint)idx >= (uint)config.sungtaeTypes.Count)
                return;
            
            SungtaeParticleTypeConfig config2 = config.sungtaeTypes[idx];
            
            SungtaeParticle item = config.poolManager.Pop<SungtaeParticle>(config.particlePoolItem);

            if (item == null)
                return;

            item.Play(
                evt.position,
                config2.material,
                () => config.poolManager.Push(item)
            );
        }
    }
}