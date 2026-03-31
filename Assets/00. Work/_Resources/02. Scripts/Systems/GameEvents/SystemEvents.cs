using Gamelib.EventSystem;

namespace _00._Work._Resources._02._Scripts.Systems.GameEvents
{
    public static class SystemEvents
    {
        public static readonly SavePrefEvent SavePref = new SavePrefEvent();
        public static readonly LoadPrefEvent LoadPref = new LoadPrefEvent();
    }
    
    public class SavePrefEvent : GameEvent { }
    public class LoadPrefEvent : GameEvent { }
}