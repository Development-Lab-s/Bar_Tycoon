using System;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Data;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI.Save
{
    [Serializable]
    public class AchieveSaveData
    {
        public AchievementType achievementType;
        public int nowAchievementDegree;
        public bool isComplete;
        public bool getAward;
        public int nowTargetData;
    }
}