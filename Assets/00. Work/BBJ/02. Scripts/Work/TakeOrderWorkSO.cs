using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "TakeOrderWork", menuName = "Tycoon/Work/TakeOrder")]
    public class TakeOrderWorkSO : WorkSO
    {
        public override async UniTask ExecuteAsync(
            ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as TakeOrderEvent;
            if (agent == null || ev == null) return;

            var seat = ev.Seat;
            await agent.MoveAsync(seat.GetNearestPoint(executor.transform.position), ct);
            ct.ThrowIfCancellationRequested();

            await agent.DoWorkAsync(seat, ct);
            ct.ThrowIfCancellationRequested();

            var seatModule = seat.GetModule<SeatModule>();
            var customer   = seatModule?.AssignedAgent as CustomerAgent;
            var ticket     = customer?.PlaceOrder(seat);

            if (ticket != null)
                OrderManager.Instance?.Register(ticket);
        }
    }
}
