using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Modules;
using BBJ.Order;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Actions;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "WaitOrderWork", menuName = "Tycoon/Work/WaitOrder")]
    public class WaitOrderWorkSO : WorkSO
    {
        [SerializeField] private float _patienceLimit = 60f;

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var actions  = executor.GetModule<AgentActionModule>();
            var seat     = customer?.AssignedSeat;
            if (customer == null || actions == null || seat == null) return WorkResult.Cancelled;

            var orderTicket = customer.PlaceOrder(seat);
            if (orderTicket == null) return WorkResult.Cancelled;

            customer.SetAwaitingOrder(true);
            _ctx.OrderChannel?.RaiseEvent(new OrderReadyForServerEvent(orderTicket));
            float deadline = Time.time + _patienceLimit;

            try
            {
                await actions.Execute<WaitAction>(a => a.ExecuteAsync(
                    () => orderTicket.WorkPhase != OrderWorkPhase.ReadyForServer || Time.time >= deadline,
                    ctx.Token));

                if (orderTicket.WorkPhase == OrderWorkPhase.ReadyForServer)
                    _ctx.OrderChannel?.RaiseEvent(new OrderCancelRequestEvent(orderTicket, CancelReason.Timeout));

                return WorkResult.Completed;
            }
            finally
            {
                customer.SetAwaitingOrder(false);
            }
        }
    }
}
