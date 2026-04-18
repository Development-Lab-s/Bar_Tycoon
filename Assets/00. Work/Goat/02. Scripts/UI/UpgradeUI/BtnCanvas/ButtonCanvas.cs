using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas
{
    public class ButtonCanvas : MonoBehaviour
    {
        public event Action<ButtonType> OnClickButton;

        private Dictionary<ButtonType, ButtonInformation> _buttonInformations;

        private void Awake()
        {
            _buttonInformations = GetComponentsInChildren<ButtonInformation>().ToDictionary(x => x.ButtonType);
            
            foreach (ButtonInformation button in _buttonInformations.Values)
            {
                button.OnClickBtn += HandleButtonClick;
            }
        }

        private void OnDestroy()
        {
            foreach (ButtonInformation button in _buttonInformations.Values)
            {
                button.OnClickBtn -= HandleButtonClick;
            }
        }

        public List<UpgradeData> GetButtonInformations(ButtonType btnType)
        {
            if (!_buttonInformations.TryGetValue(btnType, out ButtonInformation buttonInformation)) return null;
            
            return buttonInformation.UpgradeGroups;
        }

        private void HandleButtonClick(ButtonType buttonType)
        {
            OnClickButton?.Invoke(buttonType);
        }
    }
}