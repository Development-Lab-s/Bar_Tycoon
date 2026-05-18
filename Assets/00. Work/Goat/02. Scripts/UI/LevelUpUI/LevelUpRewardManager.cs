using System;
using _00._Work.Goat._02._Scripts.Exp;
using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    public class LevelUpRewardManager : MonoBehaviour
    {
        [Header("RewardSO")]
        [SerializeField] private LevelUpRewardSOs levelUpRewardSOs;
        
        [Header("References")]
        [SerializeField] private ExpManager expManager;
        
        [Header("CockTailDatabase")]
        [SerializeField] private CocktailRecipeDatabaseSO _cocktailRecipeDatabaseSo;

        public event Action<int, CocktailRecipeSO> OnCockTailAdd;
        private void Awake()
        {
            expManager.OnLevelChanged += HandleLevelChange;
        }

        private void OnDestroy()
        {
            expManager.OnLevelChanged -= HandleLevelChange;
        }

        private void HandleLevelChange(int afterLevel, int beforeLevel)
        {
            for (int level = beforeLevel + 1; level <= afterLevel; level++)
            {
                int rewardIndex = level - 2;
                
                if (rewardIndex < 0 || rewardIndex >= levelUpRewardSOs.levelUpRewardSOs.Length)
                {
                    Debug.Log($"{rewardIndex}번 인덱스 레벨보상 없음");
                    continue;
                }
                
                var rewardGroup = levelUpRewardSOs.levelUpRewardSOs[rewardIndex];

                if (rewardGroup == null)
                {
                    Debug.Log($"{rewardIndex}번 인덱스 레벨보상 없음");
                    continue;
                }
                
                foreach (CocktailRecipeSO reward in rewardGroup.cockTails)
                {
                    _cocktailRecipeDatabaseSo.AddCockTail(reward);
                    OnCockTailAdd?.Invoke(level, reward);
                }
            }
            
            //여긴 기능 해금
        }
    }
}