using System;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI
{
    public class AchievementCanvas : MonoBehaviour
    {
        [SerializeField] private AchievementSlotContainer achievementSlotContainer;
        [SerializeField] private AchieveTopUI achieveTopUI;

        private void OnEnable()
        {
            achieveTopUI.OnIsCompleteBtnClick += achievementSlotContainer.ShowContent;
        }

        private void OnDisable()
        {
            achieveTopUI.OnIsCompleteBtnClick -= achievementSlotContainer.ShowContent;
        }
    }
}