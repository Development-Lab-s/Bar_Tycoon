using _00._Work._Resources._02._Scripts.Modules;
using System.Collections;
using UnityEngine;

namespace BBJ.Tycoon
{
    [CreateAssetMenu(fileName = "CookWork", menuName = "Tycoon/Work/Cook")]
    public class CookWorkSO : WorkSO
    {
        public override WorkplaceType RequiredWorkplaceType => WorkplaceType.CookStation;

        [SerializeField] private OrderBoardSO   _orderBoard;
        [SerializeField] private CounterBoardSO _counterBoard;

        public override bool CanExecute(Workplace workplace)
            => workplace is CookStationWorkplace station && station.AssignedTicket != null;

        public override IEnumerator Execute(ModuleOwner owner, Workplace workplace)
        {
            if (workplace is not CookStationWorkplace station)
                yield break;

            yield return new MoveStep(workplace.GetNearestPoint(owner.transform.position));

            var ticket = station.AssignedTicket;
            if (ticket == null)
                yield break;

            ticket.ChangeState(OrderState.Cooking);
            yield return new WorkActionStep(ticket.Food.CookTime);

            ticket.ChangeState(OrderState.Ready);
            station.ClearTicket();
            _counterBoard.AddReadyOrder(ticket);
            _orderBoard.OnCookStationReleased();
        }
    }
}
