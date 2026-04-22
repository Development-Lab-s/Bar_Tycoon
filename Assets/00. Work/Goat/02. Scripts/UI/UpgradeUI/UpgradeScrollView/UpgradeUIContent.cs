using System;
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
        public event Action<UpgradeData> OnClickUpgrade;

        private void Awake()
        {
            InitSlots(maxSpawnCount);
        }
        
        private void InitSlots(int count)
        {
            for (int i = 0; i < count; i++) 
                CreateSlot();
        }
        
        private void CreateSlot()
        {
            UpgradeSlotUI slot = Instantiate(upgradePrefab, transform);
            slot.gameObject.SetActive(false);
            _slotList.Add(slot);
        }
    
        public void ShowUpgradeList(List<UpgradeData> dataList)
        {
            while (dataList.Count > _slotList.Count)
                CreateSlot();
            
            for (int i = 0; i < dataList.Count; i++)
            {
                UpgradeSlotUI slot = _slotList[i];
                UpgradeData data = dataList[i];
                
                string cost = data.GetCost();
                
                slot.gameObject.SetActive(true);
                slot.SetView(data, cost);
                slot.OnClickUpgrade -= HandleClickUpgrade;
                slot.OnClickUpgrade += HandleClickUpgrade;
            }
        }
        
        public void ResetSlots()
        {
            foreach (UpgradeSlotUI slot in _slotList)
            {
                slot.OnClickUpgrade -= HandleClickUpgrade;
                slot.gameObject.SetActive(false);
            }
        }
        
        private void HandleClickUpgrade(UpgradeData data)
        {
            OnClickUpgrade?.Invoke(data);
        }
    }
}