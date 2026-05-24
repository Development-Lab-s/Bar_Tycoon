using BBJ.Customer;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.Work;
using UnityEngine;

namespace BBJ.Player
{
    [CreateAssetMenu(fileName = "TakeOrderAction", menuName = "Tycoon/Player/Actions/TakeOrder")]
    public class TakeOrderActionSO : PlayerActionSO
    {
        public override void Execute(OrderTicket ticket, CustomerAgent customer, PlayerActionContext context)
        {
            var player    = context.Player;
            var oldWorker = ticket.ReservedBy;
            ticket.TrySteal(player);
            oldWorker?.GetModule<ISchedulable>()?.CancelWork();
            ticket.TryStartProgress(player);
            context.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, player));
        }
    }
}
