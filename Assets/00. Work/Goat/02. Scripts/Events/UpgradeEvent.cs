using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using Gamelib.EventSystem;

namespace _00._Work.Goat._02._Scripts.Events
{
    public static class UpgradeEvents
    {
        public static readonly UpgradeEvent UpgradeEvent = new UpgradeEvent();
    }

    public class UpgradeEvent : GameEvent
    {
        public UpgradeData upgradeData;

        public UpgradeEvent Init(UpgradeData upgradeData)
        {
            this.upgradeData = upgradeData;
            return this;
        }
    }
}