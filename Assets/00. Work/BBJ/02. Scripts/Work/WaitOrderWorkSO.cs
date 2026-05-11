using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.Schedule;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "WaitOrderWork", menuName = "Tycoon/Work/WaitOrder")]
    public class WaitOrderWorkSO : WorkSO
    {
        [SerializeField] private WorkDispatchTableSO _dispatchTable;
        [SerializeField] private float               _patienceLimit = 60f;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            var seat     = customer?.AssignedSeat;
            if (customer == null || agent == null || seat == null) return;

            customer.SetAwaitingOrder(true);

            _dispatchTable?.Dispatch(OrderWorkPhase.ReadyForServer, new TakeOrderEvent(seat), ScheduleManager.Instance);

            await agent.WaitUntilAsync(() => customer.OrderPlaced, ct, _patienceLimit);
            customer.SetAwaitingOrder(false);
        }
    }
}
