using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Movement;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "TakeSeatWork", menuName = "Tycoon/Work/TakeSeat")]
    public class TakeSeatWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO _seatType;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var customer = executor as CustomerAgent;
            var agent = executor as IActionDispatcher;
            if (customer == null || agent == null) return;

            var seat = _register
                .GetCandidates(executor.transform.position, _seatType)
                .FirstOrDefault(s => {
                    var occ = s.GetModule<OccupancyModule>();
                    return occ != null && !occ.IsOccupied && occ.TryReserve(executor, null);
                });

            if (seat == null) return;

            customer.AssignedSeat = seat;
            seat.GetModule<OccupancyModule>()?.Occupy(executor);

            var dest = seat.GetNearestPoint(executor.transform.position);
            var seatModule = seat.GetModule<SeatModule>();
            seatModule?.AssignCustomer(executor);
            var movement = customer.GetModule<IPathMovement>();

            try
            {
                await agent.MoveAsync(dest, ct);
                seatModule.Seat(executor);
            }
            catch (OperationCanceledException)
            {
                seat.GetModule<OccupancyModule>()?.Release();
                customer.AssignedSeat = null;
                throw;
            }
        }
    }
}
