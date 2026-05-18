using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using Gamelib.EventSystem;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class UpgradeUnLockEvent : GameEvent
    {
        public ButtonType buttonCanvas;

        public UpgradeUnLockEvent Init(ButtonType buttonCanvas)
        {
            this.buttonCanvas = buttonCanvas;
            return this;
        }
    }
}