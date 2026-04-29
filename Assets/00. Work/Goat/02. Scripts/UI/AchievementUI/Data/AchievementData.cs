using System;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI.Data
{
    [Serializable]
    public class AchievementData
    {
        [field: SerializeField] public AchievementDataSO AchievementDataSO { get; private set; }
        [field: SerializeField] public int NowAchievementDegree { get; private set; }
        [field: SerializeField] public bool IsComplete { get; private set; }
        [field: SerializeField] public bool GetAward { get; private set; }
        
        public event Action OnDegreeChange;
        public event Action OnComplete;
        public void AddDegree(int value)
        {
            if (IsComplete) return;
            
            NowAchievementDegree += value;

            if (NowAchievementDegree >= AchievementDataSO.TargetAchievementDegree)
            {
                NowAchievementDegree = AchievementDataSO.TargetAchievementDegree;
                Complete();
            }
            
            OnDegreeChange?.Invoke();
        }

        public void GetAwardTrue()
        {
            GetAward = true;
        }
        
        private void Complete()
        {
            IsComplete = true;
            OnComplete?.Invoke();
        }
    }
}