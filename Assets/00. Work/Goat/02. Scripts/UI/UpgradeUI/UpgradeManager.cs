using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using _00._Work.Goat._02._Scripts.Test.Coin;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Data;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.Save;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    public class UpgradeManager : MonoBehaviour
    {
        [Header("UpgradeData")]
        [field: SerializeField] public List<UpgradeCategoryData> UpgradeDataList { get; private set; }
        
        [Header("SO")]
        [SerializeField] private SaveFileNameSO  saveFileNameSo;
        [SerializeField] private EventChannelSO achievementChannelSo;
        [SerializeField] private CoinSystemSo coinSystemSo;
        
        private UpgradeSaveService  _upgradeSaveService;
        private UpgradeService _upgradeService;

        private void Awake()
        {
            JsonSaveService saveService = new JsonSaveService(saveFileNameSo);
            _upgradeService = new UpgradeService(coinSystemSo);
            _upgradeSaveService = new UpgradeSaveService(saveService,  UpgradeDataList);
            
            _upgradeSaveService.Load();
        }
        
        private void Start()
        {
            LoadUpgradeData();
        }
        
        private void LoadUpgradeData()
        {
            foreach (var data in UpgradeDataList)
            {
                foreach (var upgradeData in data.UpgradeGroups)
                {
                    data.UpgradeChannel.RaiseEvent(new UpgradeEvent().Init(upgradeData.UpgradeDataSo.TargetStat, upgradeData.UpgradeDataSo.IncreaseValue * upgradeData.CurrentLevel));   
                }
            }
        }

        public bool TryUpgrade(UpgradeData upgradeData, EventChannelSO eventChannel)
        {
            if (!_upgradeService.TryUpgrade(upgradeData))
                return false;

            _upgradeSaveService.Save();

            achievementChannelSo?.RaiseEvent( new AchievementEvent().Init(AchievementType.Upgrade, 1));

            eventChannel?.RaiseEvent(new UpgradeEvent().Init(upgradeData.UpgradeDataSo.TargetStat, upgradeData.UpgradeDataSo.IncreaseValue * upgradeData.CurrentLevel));

            return true;
        }
    }
}