using _00._Work._Resources._02._Scripts.Modules;
using System.Collections;
using UnityEngine;

namespace BBJ.Tycoon
{
    [CreateAssetMenu(fileName = "TakeOrderWork", menuName = "Tycoon/Work/TakeOrder")]
    public class TakeOrderWorkSO : WorkSO
    {
        public override WorkplaceType RequiredWorkplaceType => WorkplaceType.OrderPoint;

        [SerializeField] private OrderBoardSO _orderBoard;

        public float TakeOrderDuration = 1.5f;

        public override bool CanExecute(Workplace workplace)
            => workplace is SeatWorkplace seat && seat.IsWaitingForOrder;

        public override IEnumerator Execute(ModuleOwner owner, Workplace workplace)
        {
            yield return new MoveStep(workplace.GetNearestPoint(owner.transform.position));
            yield return new WaitStep(TakeOrderDuration);

            if (workplace is SeatWorkplace seat && seat.AssignedCustomer != null)
            {
                var order = seat.AssignedCustomer.PlaceOrder(seat);
                if (order != null)
                {
                    _orderBoard.Register(order);
                    _orderBoard.AssignOrderToCookStation(order);
                }
            }
        }
    }
}
