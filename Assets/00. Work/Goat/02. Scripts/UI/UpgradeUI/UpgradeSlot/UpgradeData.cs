using System;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot
{
    [Serializable]
    public class UpgradeData
    {
        [field: SerializeField] public UpgradeDataSO UpgradeDataSo { get; private set; }
        [field: SerializeField] public int CurrentLevel { get; private set; }

        public void UpgradeLevel()
        {
            ChangeLevel(CurrentLevel + 1);
        }
        
        public void ChangeLevel(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 0, UpgradeDataSo.MaxLevel);
        }

        public UpgradeData(UpgradeDataSO upgradeDataSo, int currentLevel)
        {
            UpgradeDataSo = upgradeDataSo;
            ChangeLevel(currentLevel);
        }
        
        public string GetCost()
        {
            string cost = CurrentLevel >= UpgradeDataSo.MaxLevel ? "MAX" : UpgradeDataSo.Costs[CurrentLevel].ToString();
            return cost;
        }

        public float GetTotalIncreaseValue()
        {
            return UpgradeDataSo.IncreaseValue * CurrentLevel;
        }
    }
}