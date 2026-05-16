using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.Order;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "TakeOrderWork", menuName = "Tycoon/Work/TakeOrder")]
    public class TakeOrderWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            var actions  = executor.GetModule<AgentActionModule>();
            var customer = ticket?.Customer as CustomerAgent;
            if (actions == null || ticket == null || customer == null) return WorkResult.Cancelled;

            if (ticket.WorkPhase != OrderWorkPhase.ReadyForServer) return WorkResult.Cancelled;
            if (!ticket.TryReserve(executor)) return WorkResult.Cancelled;

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ctx.Token, ticket.Token);
            customer.SetAssignedServer(executor);
            try
            {
                await actions.Execute<MoveAction>(
                    a => a.ExecuteAsync(ticket.Seat.GetNearestPoint(executor.transform.position), linked.Token));
                ticket.TryStartProgress(executor);

                await actions.Execute<WorkAction>(a => a.ExecuteAsync(ticket.Seat, linked.Token));

                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, executor));
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                if (!ticket.IsTerminal)
                    _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
                return WorkResult.Cancelled;
            }
            finally
            {
                customer.SetAssignedServer(null);
            }
        }
    }
}
