using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Linq;
using UnityEngine;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "TakeSeatWork", menuName = "Tycoon/Work/TakeSeat")]
    public class TakeSeatWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO     _seatType;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return WorkResult.Cancelled;

            var seat = _register
                .GetCandidates(executor.transform.position, _seatType)
                .FirstOrDefault(s => {
                    var occ = s.GetModule<OccupancyModule>();
                    return occ != null && !occ.IsOccupied && occ.TryReserve(executor, null);
                });

            if (seat == null) return WorkResult.Cancelled;

            customer.AssignedSeat = seat;
            seat.GetModule<OccupancyModule>()?.Occupy(executor);

            var dest       = seat.GetNearestPoint(executor.transform.position);
            var seatModule = seat.GetModule<SeatModule>();
            seatModule?.AssignCustomer(executor);

            try
            {
                await agent.MoveAsync(dest, ctx.Token);
                seatModule?.Seat(executor);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                seatModule?.Seat(executor);
                return WorkResult.ExternallyCompleted;
            }
            catch (OperationCanceledException)
            {
                seat.GetModule<OccupancyModule>()?.Release();
                customer.AssignedSeat = null;
                return WorkResult.Cancelled;
            }
        }
    }
}
