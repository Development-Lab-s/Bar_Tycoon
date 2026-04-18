using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.Test.Coin;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeScrollView;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using Gamelib.EventSystem;
using TMPro;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    public class UpgradeUICanvas : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private ButtonCanvas buttonCanvas;
        [SerializeField] private UpgradeUIContent content;
        [SerializeField] private CoinSystemSo coinSystemSo;
        [SerializeField] private EventChannelSO upgradeChannel;

        private UpgradeService _upgradeService;
        private List<UpgradeData> _currentDataList;

        private void Awake()
        {
            _upgradeService = new UpgradeService(coinSystemSo);
            
            buttonCanvas.OnClickButton += HandleClickButton;
            content.OnClickUpgrade += HandleClickUpgrade;
        }

        private void OnDestroy()
        {
            buttonCanvas.OnClickButton -= HandleClickButton;
            content.OnClickUpgrade -= HandleClickUpgrade;
        }

        private void HandleClickButton(ButtonType btnType)
        {
            List<UpgradeData> dataList = buttonCanvas.GetButtonInformations(btnType);

            if (dataList == null)
            {
                Debug.LogError($"{btnType} 데이터 없음");
                content.ResetSlots();
                _currentDataList = null;
                return;
            }
            
            _currentDataList = dataList;
            RefreshContent();
        }
        
        private void HandleClickUpgrade(UpgradeData upgradeData)
        {
            if (_upgradeService.TryUpgrade(upgradeData))
            {
                RefreshContent();
                upgradeChannel.RaiseEvent(UpgradeEvents.UpgradeEvent.Init(upgradeData));
            }
        }
        
        private void RefreshContent()
        {
            content.ResetSlots();
            if (_currentDataList != null)
                content.ShowUpgradeList(_currentDataList);
        }
    }
}