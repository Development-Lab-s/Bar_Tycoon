using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using _00._Work.Lusaload._02._Scripts.SO;
using Gamelib.EventSystem;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class CockTailAddEvent : GameEvent
    {
        public CocktailRecipeSO cockTailSlotSo;
        
        public CockTailAddEvent Init(CocktailRecipeSO cockTailSlotSo)
        {
            this.cockTailSlotSo = cockTailSlotSo;
            return this;
        }
    }
}