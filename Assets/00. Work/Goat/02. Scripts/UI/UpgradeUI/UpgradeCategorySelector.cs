using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    public class UpgradeCategorySelector
    {
        private readonly Dictionary<ButtonType, ButtonUpgradeData> _upgradeDataDict;
        public List<UpgradeData> CurrentDataList { get; private set; }
        public EventChannelSO CurrentEventChannel { get; private set; }

        public UpgradeCategorySelector(List<ButtonUpgradeData> upgradeDataList)
        {     
            _upgradeDataDict = new Dictionary<ButtonType, ButtonUpgradeData>();

            foreach (ButtonUpgradeData upgradeData in upgradeDataList)
            {
                if (!_upgradeDataDict.TryAdd(upgradeData.buttonType, upgradeData))
                {
                    Debug.LogError($"{upgradeData.buttonType} 중복 등록됨");
                }
            }

        }

        public bool TrySelect(ButtonType btnType)
        {
            if (!_upgradeDataDict.TryGetValue(btnType, out ButtonUpgradeData dataList))
            {
                Debug.LogWarning($"{btnType} 에 해당하는 업그레이드 데이터가 없음");
                CurrentDataList = null;
                CurrentEventChannel = null;
                return false;
            }

            CurrentEventChannel = dataList.upgradeChannel;
            CurrentDataList = dataList.upgradeGroups;
            return true;
        }
    }
}