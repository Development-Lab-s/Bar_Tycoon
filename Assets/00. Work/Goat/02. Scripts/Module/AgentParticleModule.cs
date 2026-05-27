using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Goat._02._Scripts.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Module
{
    public class AgentParticleModule : MonoBehaviour, IModule
    {
        [SerializeField] private EventChannelSO particleChannel;
        private readonly Vector2 _offset = new(0, 0.25f);
        private ModuleOwner _owner;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }
        
        public void PlayParticle(ParticleType type)
        {
            particleChannel.RaiseEvent(
                new SungtaeParticleEvent().Init(type, _owner.transform.position + (Vector3)_offset)
            );
        }
        
        public void PlayParticle(int type)
        {
            PlayParticle((ParticleType)type);
        }
        

        public void PlayParticle(ParticleType type, Vector3 position)
        {
            particleChannel.RaiseEvent(
                new SungtaeParticleEvent().Init(type, position)
            );
        }
    }
}