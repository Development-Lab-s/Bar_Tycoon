using BBJ.Customer;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.Work;
using BBJ.WorkplaceSystem.Modules;
using UnityEngine;

namespace BBJ.Player
{
    [CreateAssetMenu(fileName = "CashierAction", menuName = "Tycoon/Player/Actions/Cashier")]
    public class CashierActionSO : PlayerActionSO
    {
        public override void Execute(OrderTicket ticket, CustomerAgent customer, PlayerActionContext context)
        {
            var player    = context.Player;
            var oldWorker = ticket.ReservedBy;
            ticket.TrySteal(player);
            oldWorker?.GetModule<ISchedulable>()?.CancelWork();

            var counter = context.Register?.GetFirst(context.CounterType);
            if (counter != null)
            {
                counter.GetModule<WorkModule>()?.InstantExecute(player, ticket);
            }

            context.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, player));
        }
    }
}
