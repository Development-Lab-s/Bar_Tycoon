using BBJ.Actions;
using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ExitWork", menuName = "Tycoon/Work/Exit")]
    public class ExitWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            if (customer == null) return WorkResult.Cancelled;

            var seat    = customer.AssignedSeat;
            var actions = executor.GetModule<AgentActionModule>();

            if (seat != null)
            {
                seat.GetModule<SeatModule>()?.ClearCustomer();
                seat.GetModule<OccupancyModule>()?.Release();
                customer.AssignedSeat = null;

                var exits = _ctx.WorkplaceRegister?.GetAll(_ctx.ExitType);
                if (exits != null && exits.Count > 0 && actions != null)
                {
                    var role = executor.GetModule<SchedulingModule>()?.InteractRole;
                    try
                    {
                        await actions.Execute<MoveAction>(
                            a => a.ExecuteAsync(exits[0].GetNearestPoint(role, executor.transform.position), ctx.Token));
                    }
                    catch (System.OperationCanceledException) { }
                }
            }

            _ctx.CustomerChannel?.RaiseEvent(new CustomerLeftEvent { Customer = customer });
            return WorkResult.Completed;
        }
    }
}
