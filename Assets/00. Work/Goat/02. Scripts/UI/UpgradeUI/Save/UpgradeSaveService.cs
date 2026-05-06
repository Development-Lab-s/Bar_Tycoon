using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.SaveCode;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.Save
{
    public class UpgradeSaveService
    {
        private readonly JsonSaveService _saveService;
        private readonly List<UpgradeCategoryData> _upgradeDataList;

        public UpgradeSaveService(JsonSaveService saveService, List<UpgradeCategoryData> upgradeDataList)
        {
            _saveService = saveService;
            _upgradeDataList = upgradeDataList;
        }

        public void Save()
        {
            UpgradeSaveDataList saveDataList = new UpgradeSaveDataList();

            foreach (UpgradeCategoryData categoryData in _upgradeDataList)
            {
                foreach (UpgradeData upgradeData in categoryData.UpgradeGroups)
                {
                    saveDataList.upgradeSaveDatas.Add(new UpgradeSaveData
                    {
                        upgradeType = upgradeData.UpgradeDataSo.UpgradeType,
                        currentLevel = upgradeData.CurrentLevel
                    });
                }
            }

            _saveService.Save(saveDataList);
        }

        public void Load()
        {
            UpgradeSaveDataList saveDataList = _saveService.Load<UpgradeSaveDataList>();

            if (saveDataList == null || saveDataList.upgradeSaveDatas == null)
                return;

            Dictionary<UpgradeType, UpgradeSaveData> saveDict =
                saveDataList.upgradeSaveDatas.ToDictionary(data => data.upgradeType);

            foreach (UpgradeCategoryData categoryData in _upgradeDataList)
            {
                foreach (UpgradeData upgradeData in categoryData.UpgradeGroups)
                {
                    UpgradeType type = upgradeData.UpgradeDataSo.UpgradeType;

                    if (saveDict.TryGetValue(type, out UpgradeSaveData saveData))
                    {
                        upgradeData.ChangeLevel(saveData.currentLevel);
                    }
                }
            }
        }
    }
}