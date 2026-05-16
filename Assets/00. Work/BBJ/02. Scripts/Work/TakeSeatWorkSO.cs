using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.Order;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "TakeSeatWork", menuName = "Tycoon/Work/TakeSeat")]
    public class TakeSeatWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var actions  = executor.GetModule<AgentActionModule>();
            if (customer == null || actions == null) return WorkResult.Cancelled;

            var seat = _ctx.WorkplaceRegister
                .GetCandidates(executor.transform.position, _ctx.SeatType)
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

            bool seated = false;
            try
            {
                await actions.Execute<MoveAction>(a => a.ExecuteAsync(dest, ctx.Token));
                seatModule?.Seat(executor);
                seated = true;
                return WorkResult.Completed;
            }
            finally
            {
                if (!seated)
                {
                    seat.GetModule<OccupancyModule>()?.Release();
                    customer.AssignedSeat = null;
                }
            }
        }
    }
}
