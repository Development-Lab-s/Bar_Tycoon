using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using Gamelib.EventSystem;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class CockTailAddEvent : GameEvent
    {
        public CockTailSlotSo cockTailSlotSo;
        
        public CockTailAddEvent Init(CockTailSlotSo cockTailSlotSo)
        {
            cockTailSlotSo = cockTailSlotSo;
            return this;
        }
    }
}