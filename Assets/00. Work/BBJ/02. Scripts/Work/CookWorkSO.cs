using BBJ.Actions;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "CookWork", menuName = "Tycoon/Work/Cook")]
    public class CookWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
        {
            var actions = executor.GetModule<AgentActionModule>();
            if (actions == null || ticket == null) return WorkResult.Cancelled;

            if (!ticket.TryReserve(executor)) return WorkResult.Cancelled;

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                ctx.Token, ticket.Token);

            var role    = executor.GetModule<SchedulingModule>()?.InteractRole;
            var kitchen = _ctx.WorkplaceRegister
                .GetCandidates(executor.transform.position, _ctx.KitchenType)
                .FirstOrDefault(k => k.GetModule<OccupancyModule>()?.TryReserve(executor, null) == true);

            if (kitchen == null)
            {
                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
                return WorkResult.Cancelled;
            }

            var foodContext = executor.GetModule<FoodContextModule>();
            try
            {
                await actions.Execute<MoveAction>(
                    a => a.ExecuteAsync(kitchen.GetNearestPoint(role, executor.transform.position), linked.Token));
                ticket.TryStartProgress(executor);
                foodContext?.SetFood(ticket.Ordered);

                _ctx.OrderChannel?.RaiseEvent(new CookingStartEvent(ticket, executor));
                await actions.Execute<WorkAction>(a => a.ExecuteAsync(kitchen, linked.Token));

                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, executor));
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                if (!ticket.IsTerminal)
                    _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
                return WorkResult.Cancelled;
            }
            finally
            {
                foodContext?.ClearFood();
                kitchen.GetModule<OccupancyModule>()?.Release();
            }
        }
    }
}
