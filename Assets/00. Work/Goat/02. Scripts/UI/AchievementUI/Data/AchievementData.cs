using System;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Save;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI.Data
{
    [Serializable]
    public class AchievementData
    {
        [field: SerializeField] public AchievementDataSO AchievementDataSO { get; private set; }
        [field: SerializeField] public AchieveSaveData AchieveSaveData { get; private set; }
        
        public event Action<AchievementData> OnChanged;
        public event Action OnComplete;
        public void AddDegree(int value)
        {
            if (AchieveSaveData.isComplete)
            {
                AchieveSaveData.remainAchievementDegree += value;
                OnChanged?.Invoke(this);
                return;
            }

            AchieveSaveData.nowAchievementDegree += value;

            int targetValue = AchievementDataSO.TargetAchievementDegree[AchieveSaveData.nowTargetData];

            if (AchieveSaveData.nowAchievementDegree >= targetValue)
            {
                AchieveSaveData.remainAchievementDegree += 
                    AchieveSaveData.nowAchievementDegree - targetValue;

                AchieveSaveData.nowAchievementDegree = targetValue;
                Complete();
            }

            OnChanged?.Invoke(this);
        }

        public void ChangeAchieveData(AchieveSaveData achieveSaveData)
        {
            AchieveSaveData = achieveSaveData;
        }

        public void GetAwardTrue()
        {
            AchieveSaveData.getAward = true;

            int nextIndex = AchieveSaveData.nowTargetData + 1;

            if (nextIndex < AchievementDataSO.TargetAchievementDegree.Count)
            {
                int remainValue = AchieveSaveData.remainAchievementDegree;

                AchieveSaveData.nowTargetData = nextIndex;
                AchieveSaveData.nowAchievementDegree = 0;
                AchieveSaveData.remainAchievementDegree = 0;
                AchieveSaveData.isComplete = false;
                AchieveSaveData.getAward = false;

                AddDegree(remainValue);
            }

            OnChanged?.Invoke(this);
        }
        
        private void Complete()
        {
            AchieveSaveData.isComplete = true;
            OnComplete?.Invoke();
        }
    }
}