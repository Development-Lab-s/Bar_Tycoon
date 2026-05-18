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
        
        [Header("EventChannel")]
        [SerializeField] private EventChannelSO upgradeUnLock;
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
            upgradeUnLock.AddListener<UpgradeUnLockEvent>(HandleUpgradeUnlockEvent);
        }

        private void Start()
        {
            LoadButtonData();
        }

        private void OnDestroy()
        {
            foreach (ButtonInformation button in _buttonInformations.Values)
            {
                button.OnClickBtn -= HandleButtonClick;
            }
            upgradeUnLock.RemoveListener<UpgradeUnLockEvent>(HandleUpgradeUnlockEvent);
        }
        
        private void HandleUpgradeUnlockEvent(UpgradeUnLockEvent upgradeUnLockEvent)
        {
            OpenButton(upgradeUnLockEvent.buttonCanvas);
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
        
        public void OpenButton(ButtonType buttonType)
        {
            if (!_buttonInformations.TryGetValue(buttonType, out ButtonInformation button))
                return;

            button.SetOpen(true);
            SaveButtonData();
        }
        public void CloseButton(ButtonType buttonType)
        {
            if (!_buttonInformations.TryGetValue(buttonType, out ButtonInformation button))
                return;

            button.SetOpen(false);
            SaveButtonData();
        }
        
        private void SaveButtonData()
        {
            ButtonSaveData saveData = new ButtonSaveData();

            foreach (ButtonInformation button in _buttonInformations.Values)
            {
                ButtonOpenData openData = new ButtonOpenData
                {
                    buttonType = button.ButtonType,
                    canOpen = button.canOpen
                };

                saveData.buttonOpenDatas.Add(openData);
            }

            _saveService.Save(saveData);
        }

        
        private void LoadButtonData()
        {
            ButtonSaveData saveData = _saveService.Load<ButtonSaveData>();

            if (saveData == null)
            {
                CreateDefaultButtonData();
                SaveButtonData();
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