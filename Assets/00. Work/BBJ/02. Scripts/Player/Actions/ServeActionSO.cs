using BBJ.Customer;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.Work;
using UnityEngine;

namespace BBJ.Player
{
    [CreateAssetMenu(fileName = "ServeAction", menuName = "Tycoon/Player/Actions/Serve")]
    public class ServeActionSO : PlayerActionSO
    {
        public override void Execute(OrderTicket ticket, CustomerAgent customer, PlayerActionContext context)
        {
            var player    = context.Player;
            var oldWorker = ticket.ReservedBy;
            ticket.TrySteal(player);
            oldWorker?.GetModule<ISchedulable>()?.CancelWork();

            customer.OnFoodServed();

            ticket.TryStartProgress(player);
            context.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, player));
        }
    }
}
