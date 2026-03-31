using Gamelib.EventSystem;

namespace _00._Work._Resources._02._Scripts.Systems.GameEvents
{
    public static class InventoryEvents
    {
        public static readonly PlayerSkillInventoryChangedEvent PlayerSkillInventoryChanged = new PlayerSkillInventoryChangedEvent();
    }

    public class PlayerSkillInventoryChangedEvent : GameEvent
    {
        
    }
}