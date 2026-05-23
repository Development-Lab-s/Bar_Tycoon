using System;
using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards
{
    [Serializable]
    public struct UnlockData
    {
        public List<AbstractUnlockSO> unlockDatas;

        public void LevelUpReward()
        {
            if (unlockDatas == null) return;
            
            foreach (AbstractUnlockSO unlockData  in unlockDatas)
            {
                unlockData.LevelUpReward();
            }
        }
        public List<Vector2> GetSpawnPositions()
        {
            List<Vector2> result = new();

            if (unlockDatas == null) return result;

            foreach (AbstractUnlockSO unlockData in unlockDatas)
            {
                result.AddRange(unlockData.GetSpawnPositions());
            }

            return result;
        }
    }
}