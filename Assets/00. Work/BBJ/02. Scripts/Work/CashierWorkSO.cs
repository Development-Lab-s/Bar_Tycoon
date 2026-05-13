using BBJ.Actions;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CashierWork", menuName = "Tycoon/Work/Cashier")]
    public class CashierWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceTypeSO     _counterType;
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;
            if (agent == null || ev == null) return WorkResult.Cancelled;

            if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

            var counter = _workplaceRegister?.GetFirst(_counterType);
            if (counter == null) { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return WorkResult.Cancelled; }

            var queue = counter.GetModule<WorkplaceQueueModule>();
            if (queue == null)  { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return WorkResult.Cancelled; }

            OccupationSlot? slot = null;
            try
            {
                await agent.MoveAsync(counter.GetNearestPoint(executor.transform.position), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                ev.Ticket.TryStartProgress(executor);

                await agent.WaitUntilAsync(() => queue.HasWaiting, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                slot = queue.Dequeue();
                if (slot == null) { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return WorkResult.Cancelled; }

                await agent.DoWorkAsync(counter, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                slot.Value.NotifyProcessed();
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                slot?.NotifyProcessed();
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
                return WorkResult.ExternallyCompleted;
            }
            catch (OperationCanceledException)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                return WorkResult.Cancelled;
            }
        }
    }
}
