using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Data;
using Unity.Mathematics;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI
{
    public class AchievementSlotContainer : MonoBehaviour
    {
        [SerializeField] private AchievementSlot achievementSlot;
        private List<AchievementSlot> _achievementSlots = new();
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
                slot.gameObject.SetActive(true);
                _achievementSlots.Add(slot);
            }
        }

        private void SortArray()
        {
            _achievementSlots = _achievementSlots.OrderBy(slot => slot.MyData.IsComplete).ToList();
            for (int i = 0; i < _achievementSlots.Count; i++) 
                _achievementSlots[i].transform.SetSiblingIndex(i);
        }
        
        public void ShowContent(bool isComplete)
        {
            SortArray();
            foreach (AchievementSlot slot in _achievementSlots)
            {
                bool canShow = !slot.MyData.IsComplete || isComplete;
                slot.gameObject.SetActive(canShow);
            }   
        }
    }
}