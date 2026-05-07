using System;
using _00._Work._Resources._02._Scripts.Agents.StatSystem;
using _00._Work.Goat._02._Scripts.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UpgradeCode
{
    [RequireComponent(typeof(StatModule))]
    public class Upgrader : MonoBehaviour
    {
        [SerializeField] private EventChannelSO upgradeChannel;
        [SerializeField] private StatModule _statModule;
        private void Awake()
        {
            _statModule = GetComponent<StatModule>();

            upgradeChannel.AddListener<UpgradeEvent>(HandleUpgradeEvent);
        }

        private void HandleUpgradeEvent(UpgradeEvent obj)
        {
            _statModule.RemoveModifier(obj.statSo.AssetIndex, this);
            _statModule.AddModifier(obj.statSo.AssetIndex, this, obj.amount);
        }
    }
}