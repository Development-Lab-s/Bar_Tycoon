using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.Exp;
using _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards;
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
        [SerializeField] private CocktailRecipeDatabaseSO cocktailRecipeDatabaseSo;
        [SerializeField] private AlcoholListSO alcoholListSO;

        public event Action<int, CocktailRecipeSO> OnCockTailAdd;
        public event Action<Sprite, string, int> OnFuncAdd; 
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

                List<BaseAlcoholDataSO> alcoholList = new();
                foreach (CocktailRecipeSO reward in rewardGroup.cockTails)
                {
                    foreach (BaseAlcoholDataSO alcohole in reward.cocktailRecipeList)
                    {
                        alcoholList.Add(alcohole);
                    }
                    cocktailRecipeDatabaseSo.AddCockTail(reward);
                    OnCockTailAdd?.Invoke(afterLevel, reward);
                }

                alcoholListSO.alcoholList = alcoholListSO.alcoholList.Union(alcoholList).ToList();
                
                rewardGroup.unlockData.LevelUpReward();

                foreach (AbstractUnlockSO unlockData in rewardGroup.unlockData.unlockDatas)
                {
                    List<Sprite> sprites = unlockData.GetSprite();
                    List<string> descriptions = unlockData.GetDescription();

                    int count = Mathf.Min(sprites.Count, descriptions.Count);

                    for (int i = 0; i < count; i++)
                    {
                        OnFuncAdd?.Invoke(sprites[i], descriptions[i], afterLevel);
                    }
                }
            }
        }
    }
}