using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using Agents.StatSystem;
using Gamelib.EventSystem;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class UpgradeEvent : GameEvent
    {
        public StatSO statSo;
        public float amount;

        public UpgradeEvent Init(StatSO stat, float amount)
        {
            this.statSo = stat;
            this.amount = amount;
            return this;
        }
    }
}