using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Module
{
    public class AgentParticleModule : MonoBehaviour, IModule
    {
        private ModuleOwner _owner;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }
    }
}