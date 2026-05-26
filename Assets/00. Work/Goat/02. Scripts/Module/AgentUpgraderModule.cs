using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Goat._02._Scripts.Events;
using Agents.StatSystem;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Module
{
    public class AgentUpgraderModule : MonoBehaviour, IModule, IAfterInitModule
    {
        [SerializeField] private EventChannelSO upgradeChannel;
        private IStatModule _statModule;
        private ModuleOwner _owner;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            upgradeChannel.AddListener<UpgradeEvent>(HandleUpgradeEvent);
        }
        
        public void AfterInit()
        {
            _statModule = _owner.GetModule<IStatModule>();   
        }

        private void OnDestroy()
        {
            upgradeChannel.RemoveListener<UpgradeEvent>(HandleUpgradeEvent);
        }

        private void HandleUpgradeEvent(UpgradeEvent obj)
        {
            _statModule.RemoveModifier(obj.statSo.AssetIndex, this);
            _statModule.AddModifier(obj.statSo.AssetIndex, this, obj.amount);
        }
    }
}