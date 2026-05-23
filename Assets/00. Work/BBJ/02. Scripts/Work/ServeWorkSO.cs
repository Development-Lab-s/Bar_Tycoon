using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ServeWork", menuName = "Tycoon/Work/Serve")]
    public class ServeWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            var actions = executor.GetModule<AgentActionModule>();
            if (actions == null || ticket == null) return WorkResult.Cancelled;

            if (!ticket.TryReserve(executor)) return WorkResult.Cancelled;

            var serveStation = _ctx.WorkplaceRegister?.GetFirst(_ctx.KitchenType);
            if (serveStation == null)
            {
                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
                return WorkResult.Cancelled;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                ctx.Token, ticket.Token);

            var role = executor.GetModule<SchedulingModule>()?.InteractRole;
            try
            {
                Vector3 from = executor.transform.position;
                await actions.Execute<MoveAction>(
                    a => a.ExecuteAsync(serveStation.GetNearestPoint(role, from), linked.Token));
                ticket.TryStartProgress(executor);

                await actions.Execute<MoveAction>(
                    a => a.ExecuteAsync(ticket.Seat.GetNearestPoint(role, from), linked.Token));
                await actions.Execute<WorkAction>(
                    a => a.ExecuteAsync(ticket.Seat, linked.Token));

                var customer = ticket.Seat.GetModule<SeatModule>()?.AssignedAgent as CustomerAgent;
                customer?.OnFoodServed();

                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, executor));
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                if (!ticket.IsTerminal)
                    _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
                return WorkResult.Cancelled;
            }
        }
    }
}
