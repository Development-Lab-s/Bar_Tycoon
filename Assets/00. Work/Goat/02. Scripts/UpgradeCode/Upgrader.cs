using _00._Work._Resources._02._Scripts.Agents.StatSystem;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Goat._02._Scripts.Events;
using Agents.StatSystem;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UpgradeCode
{
    public class Upgrader : MonoBehaviour, IModule
    {
        [SerializeField] private EventChannelSO upgradeChannel;
        private IStatModule _statModule;
        public void Initialize(ModuleOwner owner)
        {
            _statModule = owner.GetModule<IStatModule>();
            upgradeChannel.AddListener<UpgradeEvent>(HandleUpgradeEvent);
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