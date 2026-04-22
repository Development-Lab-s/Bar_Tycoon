using _00._Work._Resources._02._Scripts.Modules;
using System.Collections;
using UnityEngine;

namespace BBJ.Tycoon
{
    [CreateAssetMenu(fileName = "ServeWork", menuName = "Tycoon/Work/Serve")]
    public class ServeWorkSO : WorkSO
    {
        public override WorkplaceType RequiredWorkplaceType => WorkplaceType.Counter;

        public float ServeDuration = 0.8f;

        public override bool CanExecute(Workplace workplace)
            => workplace is CounterWorkplace counter && counter.HasReadyOrder();

        public override IEnumerator Execute(ModuleOwner owner, Workplace workplace)
        {
            if (workplace is not CounterWorkplace counter)
                yield break;

            yield return new MoveStep(workplace.GetNearestPoint(owner.transform.position));
            yield return new WaitUntilStep(() => counter.HasReadyOrder());

            var ticket = counter.PickupOrder();
            if (ticket == null)
                yield break;

            yield return new MoveStep(ticket.Seat.GetNearestPoint(owner.transform.position));
            yield return new WorkActionStep(ServeDuration);

            ticket.ChangeState(OrderState.Served);
            ticket.Seat.AssignedCustomer?.OnFoodServed(ticket);
        }
    }
}
