using BBJ.Actions;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.WorkplaceSystem;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CashierWork", menuName = "Tycoon/Work/Cashier")]
    public class CashierWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceTypeSO     _counterType;
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;
            if (agent == null || ev == null) return;

            if (!ev.Ticket.TryReserve(executor)) return;

            var counter = _workplaceRegister?.GetFirst(_counterType);
            if (counter == null)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                return;
            }

            var queue = counter.GetModule<WorkplaceQueueModule>();
            if (queue == null)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                return;
            }

            try
            {
                await agent.MoveAsync(counter.GetNearestPoint(executor.transform.position), ct);
                ct.ThrowIfCancellationRequested();

                ev.Ticket.TryStartProgress(executor);

                await agent.WaitUntilAsync(() => queue.HasWaiting, ct);
                ct.ThrowIfCancellationRequested();

                var slot = queue.Dequeue();
                if (slot == null)
                {
                    ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                    return;
                }

                await agent.DoWorkAsync(counter, ct);
                ct.ThrowIfCancellationRequested();

                slot.Value.NotifyProcessed();
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
            }
            catch (System.OperationCanceledException)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                throw;
            }
        }
    }
}
