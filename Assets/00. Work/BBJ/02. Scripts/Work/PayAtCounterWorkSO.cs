using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "PayAtCounterWork", menuName = "Tycoon/Work/PayAtCounter")]
    public class PayAtCounterWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var actions  = executor.GetModule<AgentActionModule>();
            if (customer == null || actions == null) return WorkResult.Cancelled;

            customer.AssignedSeat?.GetModule<SeatModule>()?.UnSeat();

           var counter = _ctx.WorkplaceRegister?.GetFirst(_ctx.CounterType);
            if (counter == null) return WorkResult.Cancelled;

            var role = executor.GetModule<SchedulingModule>()?.InteractRole;
            await actions.Execute<MoveAction>(
                a => a.ExecuteAsync(counter.GetNearestPoint(role, executor.transform.position), ctx.Token));

            var payQueue = counter.GetModule<WorkplaceQueueModule>();
            if (payQueue == null) return WorkResult.Cancelled;

            bool paid = false;
            var slot = new OccupationSlot(
                executor.transform,
                pos => actions.Execute<MoveAction>(a => a.ExecuteAsync(pos, ctx.Token)).Forget(),
                () => { customer.OnPaymentDone(); paid = true; });
            payQueue.Enqueue(slot);

            await actions.Execute<WaitAction>(a => a.ExecuteAsync(() => paid, ctx.Token));
            return WorkResult.Completed;
        }
    }
}
