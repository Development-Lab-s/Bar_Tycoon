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

            if (AchieveSaveData.nowAchievementDegree >= AchievementDataSO.TargetAchievementDegree[AchieveSaveData.nowTargetData])
            {
                AchieveSaveData.nowAchievementDegree = AchievementDataSO.TargetAchievementDegree[AchieveSaveData.nowTargetData];
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
                AchieveSaveData.nowTargetData = nextIndex;
                AchieveSaveData.nowAchievementDegree = 0;
                AchieveSaveData.isComplete = false;
                AchieveSaveData.getAward = false;
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