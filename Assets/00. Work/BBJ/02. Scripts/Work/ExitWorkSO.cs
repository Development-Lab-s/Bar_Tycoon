using BBJ.Actions;
using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ExitWork", menuName = "Tycoon/Work/Exit")]
    public class ExitWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO     _exitType;
        [SerializeField] private EventChannelSO      _customerChannel;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var customer = executor as CustomerAgent;
            if (customer == null) return;

            var seat  = customer.AssignedSeat;
            var agent = executor as IActionDispatcher;

            if (seat != null)
            {
                seat.GetModule<SeatModule>()?.ClearCustomer();
                seat.GetModule<OccupancyModule>()?.Release();
                customer.AssignedSeat = null;

                var exits = _register?.GetAll(_exitType);
                if (exits != null && exits.Count > 0 && agent != null)
                    await agent.MoveAsync(exits[0].GetNearestPoint(executor.transform.position), ct);
            }

            _customerChannel?.RaiseEvent(new CustomerLeftEvent { Customer = customer });
        }
    }
}
