using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using UnityEditor.Search;
using BBJ.Register;
using BBJ.WorkplaceSystem;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ServeWork", menuName = "Tycoon/Work/Serve")]
    public class ServeWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO workplaceRegister;
        [SerializeField] private WorkplaceTypeSO serveStationTypeSO;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as ServeEvent;
            var seat = ev.Seat;
            var serveStation = workplaceRegister?.GetFirst(serveStationTypeSO);
            if (agent == null || ev == null) return;

            Vector3 from = executor.transform.position;

            Vector3 serveStationPos = serveStation.GetNearestPoint(from);
            Vector3 seatPos         = seat.GetNearestPoint(from);

            await agent.MoveAsync(serveStationPos, ct);
            ct.ThrowIfCancellationRequested();

            await agent.MoveAsync(seatPos, ct);
            ct.ThrowIfCancellationRequested();

            await agent.DoWorkAsync(ev.Seat, ct);
            ct.ThrowIfCancellationRequested();

            ev.Ticket.ChangeState(OrderState.Served);

            var seatModule = ev.Seat.GetModule<SeatModule>();
            var customer   = seatModule?.AssignedAgent as CustomerAgent;
            customer?.OnFoodServed();
        }
    }
}
