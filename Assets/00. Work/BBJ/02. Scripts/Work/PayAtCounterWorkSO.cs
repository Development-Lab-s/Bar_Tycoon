using BBJ.Actions;
using BBJ.Customer;
using BBJ.Register;
using BBJ.Schedule;
using BBJ.Staff;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "PayAtCounterWork", menuName = "Tycoon/Work/PayAtCounter")]
    public class PayAtCounterWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO     _counterType;
        [SerializeField] private WorkSO              _cashierWork;
        [SerializeField] private float               _patienceLimit = 60f;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return;

            var counters = _register?.GetAll(_counterType);
            if (counters == null || counters.Count == 0) return;

            var counter = counters[0];
            await agent.MoveAsync(counter.GetNearestPoint(executor.transform.position), ct);
            ct.ThrowIfCancellationRequested();

            var payQueue = counter.GetModule<WorkplaceQueueModule>();
            if (payQueue == null) return;

            bool paid = false;
            var slot = new OccupationSlot(
                executor.transform,
                pos => agent.MoveAsync(pos, ct).Forget(),
                () => { customer.OnPaymentDone(); paid = true; });
            payQueue.Enqueue(slot);

            if (_cashierWork != null)
                ScheduleManager.Instance.Request(AgentRole.Cashier, _cashierWork, new CashierEvent(counter));

            await agent.WaitUntilAsync(() => paid, ct, _patienceLimit);
        }
    }
}
