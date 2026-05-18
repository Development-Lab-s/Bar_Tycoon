using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeScrollView;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    public class UpgradeUICanvas : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private ButtonCanvas buttonCanvas;
        [SerializeField] private UpgradeUIContent content;
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private GameObject upgradeObject;
        
        private UpgradeCategorySelector _categorySelector;

        private void Awake()
        {
            _categorySelector = new UpgradeCategorySelector(upgradeManager.UpgradeDataList);
            
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
            if (upgradeManager.TryUpgrade(upgradeData, _categorySelector.CurrentEventChannel))
            {
                RefreshContent();
            }
        }
        
        private void RefreshContent()
        {
            content.ResetSlots();
            if (_categorySelector.CurrentDataList != null)
                content.ShowUpgradeList(_categorySelector.CurrentDataList);
        }

        [ContextMenu("Show UI")]
        public void ShowUI()
        {
            upgradeObject.SetActive(true);
        }

        public void ClickExitBtn()
        {
            upgradeObject.SetActive(false);
        }
    }
}