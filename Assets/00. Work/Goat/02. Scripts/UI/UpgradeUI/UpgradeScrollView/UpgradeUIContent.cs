using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeScrollView
{
    public class UpgradeUIContent : MonoBehaviour
    {
        [SerializeField] private int maxSpawnCount;
        [SerializeField] private UpgradeSlotUI upgradePrefab;
        
        private readonly List<UpgradeSlotUI> _slotList = new();

        private void Awake()
        {
            SpawnUpgradeData();
        }

        private void SpawnUpgradeData()
        {
            if (_slotList.Count > 0) return;

            for (int i = 0; i < maxSpawnCount; i++)
            {
                UpgradeSlotUI slot = Instantiate(upgradePrefab, transform);
                slot.gameObject.SetActive(false);
                _slotList.Add(slot);
            }
        }
    
        public void ShowUpgradeList(List<UpgradeData> dataList)
        {
            while (dataList.Count > _slotList.Count)
            {
                UpgradeSlotUI slot = Instantiate(upgradePrefab, transform);
                slot.gameObject.SetActive(false);
                _slotList.Add(slot);
            }
            
            for (int i = 0; i < dataList.Count; i++)
            {
                UpgradeSlotUI slot = _slotList[i];
                UpgradeData data = dataList[i];
                UpgradeDataSO so = data.upgradeDataSo;

                string title = so.Title;
                string description = so.Description;
                string cost = data.currentLevel >= so.MaxLevel ? "MAX" : so.Costs[data.currentLevel].ToString();

                slot.gameObject.SetActive(true);
                slot.SetView(title, description, cost);
            }
        }


        public void ResetSlots()
        {
            foreach (UpgradeSlotUI slot in _slotList)
            {
                slot.gameObject.SetActive(false);
            }
        }
    }
}