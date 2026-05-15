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
        [SerializeField] private EventChannelSO codexAddEventChannel;

        public event Action<int, CockTailSlotSo> OnCockTailAdd;
        private void Awake()
        {
            expManager.OnLevelChanged += HandleLevelChange;
        }

        private void OnDestroy()
        {
            expManager.OnLevelChanged -= HandleLevelChange;
        }

        private void HandleLevelChange(int level, int levelChangeCount)
        {
            int rewardIndex = level - 2;
            int startIndex = rewardIndex - (levelChangeCount - 1);
            
            for (int i = startIndex; i <= rewardIndex; i++)
            {
                
                if (i < 0 || i >= levelUpRewardSOs.levelUpRewardSOs.Length)
                {
                    Debug.Log($"{i}번 인덱스 레벨보상 없음");
                    continue;
                }
                
                var rewardGroup = levelUpRewardSOs.levelUpRewardSOs[i];
                if (rewardGroup == null)
                {
                    Debug.Log($"{i}번 인덱스 레벨보상 없음");
                    continue;
                }
                
                foreach (CockTailSlotSo reward in rewardGroup.cockTails)
                {
                    codexAddEventChannel.RaiseEvent(new CockTailAddEvent().Init(reward));
                    OnCockTailAdd?.Invoke(level, reward);
                }   
            }
            
            //여긴 기능 해금
        }
    }
}