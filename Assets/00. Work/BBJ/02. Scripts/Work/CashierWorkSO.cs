using BBJ.Actions;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CashierWork", menuName = "Tycoon/Work/Cashier")]
    public class CashierWorkSO : WorkSO
    {
        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as CashierEvent;
            if (agent == null || ev == null) return;

            var queue = ev.Counter.GetModule<WorkplaceQueueModule>();
            if (queue == null) return;

            await agent.MoveAsync(ev.Counter.GetNearestPoint(executor.transform.position), ct);
            ct.ThrowIfCancellationRequested();

            await agent.WaitUntilAsync(() => queue.HasWaiting, ct);

            var slot = queue.Dequeue();
            if (slot == null) return;

            await agent.DoWorkAsync(ev.Counter, ct);
            ct.ThrowIfCancellationRequested();

            slot.Value.NotifyProcessed();
        }
    }
}
