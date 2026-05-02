using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Data;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI
{
    public class AchievementSlotContainer : MonoBehaviour
    {
        [SerializeField] private AchievementSlot achievementSlot;
        public event Action<AchievementData> OnClickAchievementBtn;
        
        private readonly Dictionary<AchievementData, AchievementSlot> _achievementSlotsDataDict = new();
        private void Start()
        {
            InitData();
            ShowContent(false);
        }

        private void InitData()
        {
            foreach (AchievementData data  in AchievementDataManager.Instance.Achievements)
            {
                AchievementSlot slot = Instantiate(achievementSlot, transform);
                slot.SetData(data);
                slot.OnClickAchievementBtn += HandleClickAchievementBtn; 
                slot.gameObject.SetActive(true);
                _achievementSlotsDataDict.Add(data, slot);
            }
        }

        private void OnDestroy()
        {
            foreach (AchievementSlot slot in _achievementSlotsDataDict.Values)
            {
                slot.OnClickAchievementBtn -= HandleClickAchievementBtn;
            }

        }

        private void HandleClickAchievementBtn(AchievementData data)
        {
            OnClickAchievementBtn?.Invoke(data);
        }

        private void SortArray()
        {
            List<AchievementSlot> sortedSlots = _achievementSlotsDataDict.Values
                .OrderBy(slot => slot.MyData.IsComplete)
                .ToList();

            for (int i = 0; i < sortedSlots.Count; i++)
            {
                sortedSlots[i].transform.SetSiblingIndex(i);
            }
        }
        
        public void ShowContent(bool isComplete)
        {
            SortArray();
            foreach (AchievementSlot slot in _achievementSlotsDataDict.Values)
            {
                bool canShow = !slot.MyData.IsComplete || isComplete;
                slot.gameObject.SetActive(canShow);
            }
        }
    }
}