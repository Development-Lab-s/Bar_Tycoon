using _00._Work._Resources._02._Scripts.Modules;
using System.Collections;
using UnityEngine;

namespace BBJ.Tycoon
{
    [CreateAssetMenu(fileName = "CashierWork", menuName = "Tycoon/Work/Cashier")]
    public class CashierWorkSO : WorkSO
    {
        public override WorkplaceType RequiredWorkplaceType => WorkplaceType.Counter;

        public float ProcessDuration = 1.2f;

        public override bool CanExecute(Workplace workplace)
            => workplace is CounterWorkplace counter && counter.HasPayingCustomer();

        public override IEnumerator Execute(ModuleOwner owner, Workplace workplace)
        {
            if (workplace is not CounterWorkplace counter)
                yield break;

            yield return new MoveStep(workplace.GetNearestPoint(owner.transform.position));
            yield return new WaitUntilStep(() => counter.HasPayingCustomer());

            var customer = counter.DequeuePayingCustomer();
            if (customer == null)
                yield break;

            yield return new WorkActionStep(ProcessDuration);

            customer.OnPaymentDone();
        }
    }
}
