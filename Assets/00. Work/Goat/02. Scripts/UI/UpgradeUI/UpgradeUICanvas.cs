using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeScrollView;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    public class UpgradeUICanvas : MonoBehaviour
    {
        [Header("Refference")]
        [SerializeField] private ButtonCanvas buttonCanvas;
        [SerializeField] private UpgradeUIContent content;

        private List<UpgradeData> _currentDataList;

        private void Awake()
        {
            buttonCanvas.OnClickButton += HandleClickButton;
        }

        private void OnDestroy()
        {
            buttonCanvas.OnClickButton -= HandleClickButton;
        }
        
        private void HandleClickButton(ButtonType btnType)
        {
            List<UpgradeData> dataList = buttonCanvas.GetButtonInformations(btnType);

            if (dataList == null)
            {
                Debug.LogWarning($"{btnType} 데이터 없음");
                content.ResetSlots();
                _currentDataList = null;
                return;
            }
            
            _currentDataList = dataList;
            content.ResetSlots();
            content.ShowUpgradeList(dataList);
        }
    }
}