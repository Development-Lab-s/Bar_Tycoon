using System;
using _00._Work.Goat._02._Scripts.Coin;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot
{
    [Serializable]
    public class UpgradeData
    {
        [field: SerializeField] public UpgradeDataSO UpgradeDataSo { get; private set; }
        [field: SerializeField] public int           CurrentLevel  { get; private set; }

        public void UpgradeLevel() => ChangeLevel(CurrentLevel + 1);

        public void ChangeLevel(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 0, UpgradeDataSo.MaxLevel);
        }

        public UpgradeData(UpgradeDataSO upgradeDataSo, int currentLevel)
        {
            UpgradeDataSo = upgradeDataSo;
            ChangeLevel(currentLevel);
        }

        public string GetLevel(int offset)
        {
            int target = CurrentLevel + offset;
            if (target > UpgradeDataSo.MaxLevel)  return $"현재 레벨: {CurrentLevel}";
            if (target >= UpgradeDataSo.MaxLevel) return "MAX";
            return (target + 1).ToString();
        }

        public string GetCost()
        {
            if (CurrentLevel >= UpgradeDataSo.MaxLevel) return "MAX";
            return CoinFormatter.Format(UpgradeDataSo.GetCost(CurrentLevel));
        }

        public float GetTotalIncreaseValue() => UpgradeDataSo.IncreaseValue * CurrentLevel;
    }
}
