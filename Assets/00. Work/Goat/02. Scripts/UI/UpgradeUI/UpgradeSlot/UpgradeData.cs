using System;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot
{
    [Serializable]
    public class UpgradeData
    {
        public UpgradeDataSO upgradeDataSo;
        public int currentLevel;

        public UpgradeData(UpgradeDataSO upgradeDataSo, int currentLevel)
        {
            this.upgradeDataSo = upgradeDataSo;
            this.currentLevel = currentLevel;
        }
        
        public string GetCost()
        {
            string cost = currentLevel >= upgradeDataSo.MaxLevel ? "MAX" : upgradeDataSo.Costs[currentLevel].ToString();
            return cost;
        }
    }
}