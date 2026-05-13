using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ServeWork", menuName = "Tycoon/Work/Serve")]
    public class ServeWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;
        [SerializeField] private WorkplaceTypeSO     _serveStationTypeSO;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;

            if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

            var serveStation = _workplaceRegister?.GetFirst(_serveStationTypeSO);
            if (serveStation == null) return WorkResult.Cancelled;

            Vector3 from = executor.transform.position;

            try
            {
                await agent.MoveAsync(serveStation.GetNearestPoint(from), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();
                ev.Ticket.TryStartProgress(executor);

                await agent.MoveAsync(ev.Ticket.Seat.GetNearestPoint(from), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();
                await agent.DoWorkAsync(ev.Ticket.Seat, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }

        public override void OnResult(WorkResult result, ModuleOwner executor, GameEvent context)
        {
            var ev       = context as OrderWorkEvent;
            var customer = ev.Ticket.Seat.GetModule<SeatModule>()?.AssignedAgent as CustomerAgent;

            if (result != WorkResult.Cancelled)
            {
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
                customer?.OnFoodServed();
            }
            else
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
            }
        }
    }
}
