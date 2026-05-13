using Gamelib.EventSystem;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class ExpEvent : GameEvent
    {
        public int amount;
        
        public ExpEvent Init(int amount)
        {
            this.amount = amount;
            return this;
        }
    }
}