using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas
{
    public class ButtonInformation : MonoBehaviour
    {
        [field: SerializeField] public ButtonType ButtonType { get; private set; }
        [SerializeField] private Button button;
        
        [Header("Upgrade Data")]
        [field: SerializeField] public List<UpgradeData> UpgradeGroups { get; private set; }
        
        public event Action<ButtonType> OnClickBtn;
        
        private void OnEnable()
        {
            button.onClick.AddListener(HandleClickBtn);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(HandleClickBtn);
        }

        private void HandleClickBtn()
        {
            OnClickBtn?.Invoke(ButtonType);
        }
    }
}