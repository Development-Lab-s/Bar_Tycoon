using _00._Work.Goat._02._Scripts.Coin;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    public class UpgradeService
    {
        private readonly CoinManager _coinManager;
     
        public UpgradeService(CoinManager coinManager)
        {
            _coinManager = coinManager;
        }

        public bool TryUpgrade(UpgradeData upgradeData)
        {
            if (upgradeData.CurrentLevel >= upgradeData.UpgradeDataSo.MaxLevel)
            {
                Debug.Log("이미 업그레이드가 다 됐습니다");
                return false;
            }

            int cost = upgradeData.UpgradeDataSo.Costs[upgradeData.CurrentLevel];

            if (!_coinManager.TryUseCoin(cost))
            {
                Debug.Log("돈이 부족합니다");
                return false;
            }
            
            upgradeData.UpgradeLevel();
            return true;
        }
    }
}