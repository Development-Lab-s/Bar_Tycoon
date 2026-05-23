using BBJ.Actions;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;
using BBJ.WorkplaceSystem;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CashierWork", menuName = "Tycoon/Work/Cashier")]
    public class CashierWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            var actions = executor.GetModule<AgentActionModule>();
            if (actions == null || ticket == null) return WorkResult.Cancelled;

            if (!ticket.TryReserve(executor)) return WorkResult.Cancelled;

            var role    = executor.GetModule<SchedulingModule>()?.InteractRole;
            var counter = _ctx.WorkplaceRegister?.GetFirst(_ctx.CounterType);
            if (counter == null)
            {
                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
                return WorkResult.Cancelled;
            }

            var queue = counter.GetModule<WorkplaceQueueModule>();
            if (queue == null)
            {
                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
                return WorkResult.Cancelled;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                ctx.Token, ticket.Token);

            OccupationSlot? slot      = null;
            bool            processed = false;
            try
            {
                await actions.Execute<MoveAction>(
                    a => a.ExecuteAsync(counter.GetNearestPoint(role, executor.transform.position), linked.Token));
                ticket.TryStartProgress(executor);
                var foodContext = executor.GetModule<FoodContextModule>();
                foodContext?.SetFood(ticket.Ordered);
                await actions.Execute<WaitAction>(
                    a => a.ExecuteAsync(() => queue.HasWaiting, linked.Token));

                slot = queue.Dequeue();
                if (slot == null)
                {
                    _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
                    return WorkResult.Cancelled;
                }

                await actions.Execute<WorkAction>(
                    a => a.ExecuteAsync(counter, linked.Token));

                slot.Value.NotifyProcessed();
                processed = true;

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
                if (!processed && slot.HasValue)
                    slot.Value.NotifyProcessed();
            }
        }
    }
}
