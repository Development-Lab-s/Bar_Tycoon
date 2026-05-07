using BBJ.Actions;
using BBJ.Order;
using BBJ.Register;
using BBJ.Schedule;
using BBJ.Staff;
using BBJ.WorkplaceSystem;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Linq;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CookWork", menuName = "Tycoon/Work/Cook")]
    public class CookWorkSO : WorkSO
    {
        [SerializeField] private OrderQueueSO        _readyQueue;
        [SerializeField] private WorkplaceTypeSO     _kitchenType;
        [SerializeField] private WorkSO              _serveWork;

        [SerializeField] private WorkplaceRegisterSO workplaceRegister;
        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as CookEvent;
            if (agent == null || ev == null) return;

            var kitchen = workplaceRegister
                .GetCandidates(executor.transform.position, _kitchenType)
                .FirstOrDefault(k => k.TryReserve(executor, null));

            if (kitchen == null) return;

            try
            {
                await agent.MoveAsync(kitchen.GetNearestPoint(executor.transform.position), ct);
                ct.ThrowIfCancellationRequested();

                ev.Ticket.ChangeState(OrderState.Cooking);
                await agent.DoWorkAsync(kitchen, ct);
                ct.ThrowIfCancellationRequested();


                //var serveStation = workplaceRegister?.GetFirst(serveStationTypeSO);
                //Vector3 serveStationPos = serveStation.GetNearestPoint(executor.transform.position);
                //await agent.MoveAsync(serveStationPos, ct);
                //ct.ThrowIfCancellationRequested();

                ev.Ticket.ChangeState(OrderState.Ready);
                _readyQueue?.Enqueue(ev.Ticket);

                if (_serveWork != null)
                    ScheduleManager.Instance.Request(
                        AgentRole.Server, _serveWork,
                        new ServeEvent(ev.Ticket, ev.Ticket.Seat));
            }
            finally
            {
                kitchen.Release();
            }
        }
    }
}
