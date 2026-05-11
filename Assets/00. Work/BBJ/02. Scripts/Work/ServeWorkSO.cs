using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using System;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ServeWork", menuName = "Tycoon/Work/Serve")]
    public class ServeWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;
        [SerializeField] private WorkplaceTypeSO     _serveStationTypeSO;

        public override async UniTask ExecuteAsync(
            ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;

            if (!ev.Ticket.TryReserve(executor)) return;

            var serveStation = _workplaceRegister?.GetFirst(_serveStationTypeSO);
            if (serveStation == null)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                return;
            }

            Vector3 from            = executor.transform.position;
            Vector3 serveStationPos = serveStation.GetNearestPoint(from);
            Vector3 seatPos         = ev.Ticket.Seat.GetNearestPoint(from);

            try
            {
                await agent.MoveAsync(serveStationPos, ct);
                ct.ThrowIfCancellationRequested();

                ev.Ticket.TryStartProgress(executor);

                await agent.MoveAsync(seatPos, ct);
                ct.ThrowIfCancellationRequested();

                await agent.DoWorkAsync(ev.Ticket.Seat, ct);
                ct.ThrowIfCancellationRequested();

                ev.OrderManager.NotifyComplete(ev.Ticket, executor);

                SeatModule seatModule = ev.Ticket.Seat.GetModule<SeatModule>();
                CustomerAgent customer   = seatModule?.AssignedAgent as CustomerAgent;
                customer?.OnFoodServed();
            }
            catch (OperationCanceledException)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                throw;
            }
        }
    }
}
