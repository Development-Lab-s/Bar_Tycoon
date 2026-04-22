using System;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.Test.Coin;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    public class UpgradeService
    {
        private readonly CoinSystemSo _coinSystemSo;
     
        public UpgradeService(CoinSystemSo coinSystemSo)
        {
            _coinSystemSo = coinSystemSo;
        }

        public bool TryUpgrade(UpgradeData upgradeData)
        {
            if (upgradeData.currentLevel >= upgradeData.upgradeDataSo.MaxLevel)
            {
                Debug.Log("이미 업그레이드가 다 됐습니다");
                return false;
            }

            int cost = upgradeData.upgradeDataSo.Costs[upgradeData.currentLevel];

            if (cost > _coinSystemSo.Coin)
            {
                Debug.Log("돈이 부족합니다");
                return false;
            }
            
            _coinSystemSo.PlusCoin(-cost);
            upgradeData.currentLevel++;
            return true;
        }
    }
}