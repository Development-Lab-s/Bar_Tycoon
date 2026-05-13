using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.Schedule;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "WaitOrderWork", menuName = "Tycoon/Work/WaitOrder")]
    public class WaitOrderWorkSO : WorkSO
    {
        [SerializeField] private WorkDispatchTableSO _dispatchTable;
        [SerializeField] private float               _patienceLimit = 60f;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            var seat     = customer?.AssignedSeat;
            if (customer == null || agent == null || seat == null) return WorkResult.Cancelled;

            try
            {
                customer.SetAwaitingOrder(true);
                _dispatchTable?.Dispatch(OrderWorkPhase.ReadyForServer, new TakeOrderEvent(seat), ScheduleManager.Instance);
                await agent.WaitUntilAsync(() => customer.OrderPlaced, ctx.Token, _patienceLimit);
                customer.SetAwaitingOrder(false);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                customer.SetAwaitingOrder(false);
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
