using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "TakeOrderWork", menuName = "Tycoon/Work/TakeOrder")]
    public class TakeOrderWorkSO : WorkSO
    {
        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as TakeOrderEvent;
            if (agent == null || ev == null) return WorkResult.Cancelled;

            var seat = ev.Seat;
            try
            {
                await agent.MoveAsync(seat.GetNearestPoint(executor.transform.position), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                await agent.DoWorkAsync(seat, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                var seatModule = seat.GetModule<SeatModule>();
                var customer   = seatModule?.AssignedAgent as CustomerAgent;
                var ticket     = customer?.PlaceOrder(seat);

                if (ticket != null)
                    OrderManager.Instance?.Register(ticket);

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
