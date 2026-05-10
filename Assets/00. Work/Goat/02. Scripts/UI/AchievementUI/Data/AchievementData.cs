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
            if (AchieveSaveData.isComplete) return;
            
            AchieveSaveData.nowAchievementDegree += value;

            if (AchieveSaveData.nowAchievementDegree >= AchievementDataSO.TargetAchievementDegree)
            {
                AchieveSaveData.nowAchievementDegree = AchievementDataSO.TargetAchievementDegree;
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
            OnChanged?.Invoke(this);
        }
        
        private void Complete()
        {
            AchieveSaveData.isComplete = true;
            OnComplete?.Invoke();
        }
    }
}