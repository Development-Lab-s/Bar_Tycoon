using _00._Work._Resources._02._Scripts.Agents.Players;
using Gamelib.EventSystem;

namespace _00._Work._Resources._02._Scripts.Systems.GameEvents
{
    public static class PlayerEvents
    {
        public static readonly ActivePlayerEvent ActivePlayerEvent = new ActivePlayerEvent();
        public static readonly PlayerDataSetUpEvent PlayerDataSetUpEvent = new PlayerDataSetUpEvent();
    }
    
    public class PlayerDataSetUpEvent : GameEvent
    {
        public PlayerData PlayerData;

        public PlayerDataSetUpEvent Init(PlayerData playerData)
        {
            PlayerData = playerData;
            return this;
        }
    }


    public class ActivePlayerEvent : GameEvent
    {
        public bool IsActive { get; private set; }

        public ActivePlayerEvent Init(bool isActive)
        {
            IsActive = isActive;
            return this;
        }
    }

    public class AddExpEvent : GameEvent
    {
        public int Amount { get; private set; }

        public AddExpEvent Init(int amount)
        {
            Amount = amount;
            return this;
        }
    }

    public class LevelUpEvent : GameEvent
    {
        public int NewLevel { get; private set; }

        public LevelUpEvent Init(int newLevel)
        {
            NewLevel = newLevel;
            return this;
        }
    }
}