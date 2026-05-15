using System;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.Exp;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    public class LevelUpRewardManager : MonoBehaviour
    {
        [Header("RewardSO")]
        [SerializeField] private LevelUpRewardSOs levelUpRewardSOs;
        
        [Header("References")]
        [SerializeField] private ExpManager expManager;
        
        [Header("EventChannel")]
        [SerializeField] private EventChannelSO cockTailAddEventChannel;

        public event Action<CockTailSlotSo> OnCockTailAdd;

        private void Awake()
        {
            expManager.OnLevelChanged += HandleLevelChange;
        }

        private void OnDestroy()
        {
            expManager.OnLevelChanged -= HandleLevelChange;
        }

        private void HandleLevelChange(int level)
        {
            foreach (CockTailSlotSo reward in levelUpRewardSOs.levelUpRewardSOs[level-2].cockTails)
            {
                cockTailAddEventChannel.RaiseEvent(new CockTailAddEvent().Init(reward));
                OnCockTailAdd?.Invoke(reward);
            }
            
            //여긴 기능 해금
        }
    }
}