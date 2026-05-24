using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;

namespace BBJ.Player
{
    public class PlayerActionContext
    {
        public PlayerOrderHandle    Player;
        public WorkplaceRegisterSO  Register;
        public WorkplaceTypeSO      CounterType;
        public EventChannelSO       OrderChannel;
    }
}
