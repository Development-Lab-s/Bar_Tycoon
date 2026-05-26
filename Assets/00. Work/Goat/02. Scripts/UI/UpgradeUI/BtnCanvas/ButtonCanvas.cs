using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas.ButtonDatas;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas
{
    public class ButtonCanvas : MonoBehaviour
    {
        [Header("Save")]
        [SerializeField] private SaveFileNameSO saveFileName;
        public event Action<ButtonType> OnClickButton;

        private Dictionary<ButtonType, ButtonInformation> _buttonInformations;
        private JsonSaveService _saveService;
        private void Awake()
        {
            _saveService = new JsonSaveService(saveFileName);
            
            _buttonInformations = GetComponentsInChildren<ButtonInformation>().ToDictionary(x => x.ButtonType);
            
            foreach (ButtonInformation button in _buttonInformations.Values)
            {
                button.OnClickBtn += HandleButtonClick;
            }
        }

        private void OnEnable()
        {
            CreateDefaultButtonData();
            LoadButtonData();   
        }

        private void HandleButtonClick(ButtonType buttonType)
        {
            OnClickButton?.Invoke(buttonType);

            foreach (ButtonInformation button in _buttonInformations.Values)
            {
                if (button.ButtonType == buttonType)
                {
                    button.Active();
                }
                else
                {
                    button.NoActive();
                }
            }
        }
        
        private void LoadButtonData()
        {
            ButtonSaveData saveData = _saveService.Load<ButtonSaveData>();

            if (saveData == null)
            {
                CreateDefaultButtonData();
                return;
            }

            foreach (ButtonOpenData openData in saveData.buttonOpenDatas)
            {
                if (_buttonInformations.TryGetValue(openData.buttonType, out ButtonInformation button))
                {
                    button.SetOpen(openData.canOpen);
                }
            }
        }
        private void CreateDefaultButtonData()
        {
            foreach (ButtonInformation button in _buttonInformations.Values)
            {
                button.SetOpen(false);
            }
        }

    }
}