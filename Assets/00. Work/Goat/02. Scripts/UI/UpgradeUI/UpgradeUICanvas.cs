using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.Test.Coin;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeScrollView;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    public class UpgradeUICanvas : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private ButtonCanvas buttonCanvas;
        [SerializeField] private UpgradeUIContent content;
        [SerializeField] private CoinSystemSo coinSystemSo;
        
        [Header("UpgradeData")]
        [SerializeField] private List<UpgradeCategoryData> upgradeDataList;
        
        private UpgradeCategorySelector _categorySelector;
        private UpgradeService _upgradeService;

        private void Awake()
        {
            _categorySelector = new UpgradeCategorySelector(upgradeDataList);
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
            _categorySelector.TrySelect(btnType);
            RefreshContent();
        }
        
        private void HandleClickUpgrade(UpgradeData upgradeData)
        {
            if (_upgradeService.TryUpgrade(upgradeData))
            {
                RefreshContent();
                _categorySelector.CurrentEventChannel?.RaiseEvent(new UpgradeEvent().Init(upgradeData));
            }
        }
        
        private void RefreshContent()
        {
            content.ResetSlots();
            if (_categorySelector.CurrentDataList != null)
                content.ShowUpgradeList(_categorySelector.CurrentDataList);
        }
    }
}