using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.Staff;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "TakeOrderWork", menuName = "Tycoon/Work/TakeOrder")]
    public class TakeOrderWorkSO : WorkSO
    {
        [SerializeField] private WorkSO _cookWork;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as TakeOrderEvent;
            if (agent == null || ev == null) return;

            var seat = ev.Seat;

            Vector3 from = executor.transform.position;
            Vector3 destination = seat.GetNearestPoint(from);
            await agent.MoveAsync(destination, ct);
            ct.ThrowIfCancellationRequested();

            await agent.DoWorkAsync(seat, ct);
            ct.ThrowIfCancellationRequested();

            var seatModule = seat.GetModule<SeatModule>();
            var customer   = seatModule?.AssignedAgent as CustomerAgent;
            var ticket     = customer?.PlaceOrder(seat);

            if (ticket != null)
            {
                OrderManager.Instance?.Register(ticket);
                if (_cookWork != null)
                    ScheduleManager.Instance.Request(AgentRole.Cooker, _cookWork, new CookEvent(ticket));
            }
        }
    }
}
