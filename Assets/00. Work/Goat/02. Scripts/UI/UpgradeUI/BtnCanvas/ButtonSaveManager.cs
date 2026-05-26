using System;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas.ButtonDatas;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas
{
    public class ButtonSaveManager : MonoBehaviour
    {
        [Header("Save")]
        [SerializeField] private SaveFileNameSO saveFileName;
        
        [Header("EventChannel")]
        [SerializeField] private EventChannelSO upgradeUnLock;
        
        private JsonSaveService _saveService;

        private void Awake()
        {
            _saveService = new JsonSaveService(saveFileName);
            
            upgradeUnLock.AddListener<UpgradeUnLockEvent>(HandleUpgradeUnlockEvent);
        }

        private void OnDestroy()
        {
            upgradeUnLock.RemoveListener<UpgradeUnLockEvent>(HandleUpgradeUnlockEvent);
        }

        private void HandleUpgradeUnlockEvent(UpgradeUnLockEvent upgradeUnLockEvent)
        {
            ButtonSaveData saveData = _saveService.Load<ButtonSaveData>();

            if (saveData == null)
            {
                saveData = new ButtonSaveData();
            }

            bool found = false;

            foreach (ButtonOpenData buttonOpenData in saveData.buttonOpenDatas)
            {
                if (buttonOpenData.buttonType == upgradeUnLockEvent.buttonCanvas)
                {
                    buttonOpenData.canOpen = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                saveData.buttonOpenDatas.Add(new ButtonOpenData
                {
                    buttonType = upgradeUnLockEvent.buttonCanvas,
                    canOpen = true
                });
            }

            _saveService.Save(saveData);
        }
        
    }
}