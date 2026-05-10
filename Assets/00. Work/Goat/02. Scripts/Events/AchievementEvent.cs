using _00._Work.Goat._02._Scripts.UI.AchievementUI;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Data;
using Gamelib.EventSystem;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class AchievementEvent : GameEvent
    {
        public AchievementType achievementType;
        public int amount;

        public AchievementEvent Init(AchievementType achievementType, int amount)
        {
            this.achievementType = achievementType;
            this.amount = amount;
            return this;
        }
    }
}