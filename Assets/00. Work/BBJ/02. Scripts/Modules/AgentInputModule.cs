using System;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.Module;
using _00._Work.PCM._02._Scripts;

namespace BBJ.Modules
{
    public class AgentInputModule : AbstructContractObject, IAgentInput
    {
        public event Action OnInteracted;

        private ModuleOwner _moduleOwner;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _moduleOwner = owner;
            OnLike.RemoveListener(HandleLike);
            OnLike.AddListener(HandleLike);
        }

        public override void ExcuteClick()
        {
            base.ExcuteClick();
            OnInteracted?.Invoke();
        }

        private void HandleLike(int _)
        {
            _moduleOwner?.GetModule<AgentParticleModule>()?.PlayParticle(ParticleType.HEART);
            OnInteracted?.Invoke();
        }
    }
}
