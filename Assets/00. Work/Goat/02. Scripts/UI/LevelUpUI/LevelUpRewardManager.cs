using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Exp;
using _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards;
using _00._Work.Lusaload._02._Scripts.SO;
using BBJ.GridSystem.Objects;
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
        
        [Header("CockTailDatabase")]
        [SerializeField] private CocktailRecipeDatabaseSO _cocktailRecipeDatabaseSo;

        public event Action<int, CocktailRecipeSO> OnCockTailAdd;
        public event Action<Sprite> OnFuncAdd; 
        public event Action<List<Vector2>> OnObjectAddCameraMove; 
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
                
                rewardGroup.unlockData.LevelUpReward();
                
                List<Vector2> objectPositions = rewardGroup.unlockData.GetSpawnPositions();

                if (objectPositions.Count > 0)
                {
                    OnObjectAddCameraMove?.Invoke(objectPositions);
                }

                foreach (AbstractUnlockSO unlockData in rewardGroup.unlockData.unlockDatas)
                {
                    foreach (Sprite sprite in unlockData.GetSprite())
                    {
                        OnFuncAdd?.Invoke(sprite);
                    }
                }
            }
        }
    }
}