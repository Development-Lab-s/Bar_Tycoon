using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Schedule;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Player
{
    [CreateAssetMenu(fileName = "PlayerOrderHandler", menuName = "Tycoon/Player/OrderHandler")]
    public class PlayerOrderHandlerSO : ScriptableObject
    {
        [SerializeField] private EventChannelSO _orderChannel;

        public void OnCustomerClicked(CustomerAgent customer)
        {
            if (customer == null) return;
            if (!customer.IsReadyForOrder) return;

            var ticket = customer.ActiveTicket;
            if (ticket == null || ticket.IsTerminal) return;
            if (ticket.WorkPhase != OrderWorkPhase.ReadyForServer) return;

            customer.AssignedServer?.GetModule<ISchedulable>()?.CancelWork();
            _orderChannel?.RaiseEvent(new PlayerOrderTakeEvent(ticket));
        }
    }
}
