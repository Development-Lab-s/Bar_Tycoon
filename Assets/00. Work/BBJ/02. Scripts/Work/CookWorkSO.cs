using BBJ.Actions;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Linq;
using System.Threading;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "CookWork", menuName = "Tycoon/Work/Cook")]
    public class CookWorkSO : WorkSO
    {
        [UnityEngine.SerializeField] private WorkplaceTypeSO     _kitchenType;
        [UnityEngine.SerializeField] private WorkplaceRegisterSO _workplaceRegister;

        public override async UniTask ExecuteAsync(
            ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;
            if (agent == null || ev == null) return;

            var actor = executor;
            if (!ev.Ticket.TryReserve(actor)) return;

            var kitchen = _workplaceRegister
                .GetCandidates(executor.transform.position, _kitchenType)
                .FirstOrDefault(k => k.GetModule<OccupancyModule>()?.TryReserve(executor, null) == true);

            if (kitchen == null)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, actor);
                return;
            }

            var foodContext = executor.GetModule<FoodContextModule>();
            try
            {
                await agent.MoveAsync(
                    kitchen.GetNearestPoint(executor.transform.position), ct);
                ct.ThrowIfCancellationRequested();

                ev.Ticket.TryStartProgress(actor);

                foodContext?.SetFood(ev.Ticket.Food);
                await agent.DoWorkAsync(kitchen, ct);
                ct.ThrowIfCancellationRequested();

                ev.OrderManager.NotifyComplete(ev.Ticket, actor);
            }
            catch (System.OperationCanceledException)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, actor);
                throw;
            }
            finally
            {
                foodContext?.ClearFood();
                kitchen.GetModule<OccupancyModule>()?.Release();
            }
        }
    }
}
