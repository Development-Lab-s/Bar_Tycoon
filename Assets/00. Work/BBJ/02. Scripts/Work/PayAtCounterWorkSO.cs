using BBJ.Actions;
using BBJ.Customer;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "PayAtCounterWork", menuName = "Tycoon/Work/PayAtCounter")]
    public class PayAtCounterWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO     _counterType;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return WorkResult.Cancelled;

            try
            {
                customer.AssignedSeat.GetModule<SeatModule>().UnSeat();
                var counter = _register?.GetFirst(_counterType);
                await agent.MoveAsync(counter.GetNearestPoint(executor.transform.position), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                var payQueue = counter.GetModule<WorkplaceQueueModule>();
                if (payQueue == null) return WorkResult.Cancelled;

                bool paid = false;
                var slot = new OccupationSlot(
                    executor.transform,
                    pos => agent.MoveAsync(pos, ctx.Token).Forget(),
                    () => { customer.OnPaymentDone(); paid = true; });
                payQueue.Enqueue(slot);

                await agent.WaitUntilAsync(() => paid, ctx.Token);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
