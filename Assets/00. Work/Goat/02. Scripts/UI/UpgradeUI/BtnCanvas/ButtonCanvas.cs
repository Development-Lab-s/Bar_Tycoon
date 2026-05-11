using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas
{
    public class ButtonCanvas : MonoBehaviour
    {
        [field: SerializeField] public ButtonType MyButtonType { get; private set; }
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

        private void HandleButtonClick(ButtonType buttonType)
        {
            OnClickButton?.Invoke(buttonType);
        }
    }
}