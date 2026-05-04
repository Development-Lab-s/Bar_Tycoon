using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using _00._Work.Goat._02._Scripts.Test.Coin;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.Save;
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
        
        [Header("SO")]
        [SerializeField] private SaveFileNameSO  saveFileNameSo;
            
        private JsonSaveService _saveService;
        private UpgradeCategorySelector _categorySelector;
        private UpgradeService _upgradeService;
        private UpgradeSaveService  _upgradeSaveService;

        private void Awake()
        {
            _saveService = new JsonSaveService(saveFileNameSo);
            _categorySelector = new UpgradeCategorySelector(upgradeDataList);
            _upgradeService = new UpgradeService(coinSystemSo);
            _upgradeSaveService = new UpgradeSaveService(_saveService,  upgradeDataList);
            
            _upgradeSaveService.Load();
            
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
                
                _upgradeSaveService.Save();
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