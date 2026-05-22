using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.Work;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;

namespace BBJ.Player
{
    [CreateAssetMenu(fileName = "PlayerOrderHandler", menuName = "Tycoon/Player/OrderHandler")]
    public class PlayerOrderHandlerSO : ScriptableObject
    {
        [SerializeField] private EventChannelSO _orderChannel;
        [SerializeField] private EventChannelSO _uiChannel;
        [SerializeField] private WorkContextSO  _workCtx;

        private PlayerOrderHandle _player;

        public void RuntimeInit(PlayerOrderHandle player) => _player = player;

        public void OnCustomerClicked(CustomerAgent customer)
        {
            if (customer == null) return;

            var ticket = customer.ActiveTicket;
            if (ticket == null || ticket.IsTerminal) return;

            if (!ticket.IsPlayerActionable)
            {
                _uiChannel?.RaiseEvent(new MessageEvent("지금은 처리할 수 없습니다."));
                return;
            }

            switch (ticket.WorkPhase)
            {
                case OrderWorkPhase.ReadyForServer:  HandleTakeOrder(customer, ticket); break;
                case OrderWorkPhase.ReadyForServe:   HandleServe(customer, ticket);     break;
                case OrderWorkPhase.ReadyForCashier: HandleCashier(customer, ticket);   break;
            }
        }

        private void HandleTakeOrder(CustomerAgent customer, OrderTicket ticket)
        {
            customer.AssignedServer?.GetModule<ISchedulable>()?.CancelWork();
            _orderChannel?.RaiseEvent(new PlayerOrderTakeEvent(ticket));
        }

        private void HandleServe(CustomerAgent customer, OrderTicket ticket)
        {
            if (_player == null) return;

            var oldWorker = ticket.ReservedBy;
            ticket.TrySteal(_player);
            oldWorker?.GetModule<ISchedulable>()?.CancelWork();

            customer.OnFoodServed();

            ticket.TryStartProgress(_player);
            _orderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, _player));
        }

        private void HandleCashier(CustomerAgent customer, OrderTicket ticket)
        {
            if (_player == null || _workCtx == null) return;

            var oldWorker = ticket.ReservedBy;
            ticket.TrySteal(_player);
            oldWorker?.GetModule<ISchedulable>()?.CancelWork();

            var counter = _workCtx.WorkplaceRegister?.GetFirst(_workCtx.CounterType);
            if (counter == null) return;

            Vector3 particlePos = customer.transform.position;
            counter.GetModule<WorkplaceQueueModule>()?.TryCompleteSlotByOwner(customer.transform);
            HandleCashierAsync(counter, ticket, particlePos).Forget();
        }

        private async UniTaskVoid HandleCashierAsync(
            Workplace counter, OrderTicket ticket, Vector3 particlePos)
        {
            var foodCtx    = _player.GetModule<FoodContextModule>();
            var workModule = counter.GetModule<WorkModule>();

            foodCtx?.SetFood(ticket.Ordered);

            if (workModule != null)
                await workModule.RunAsync(_player, CancellationToken.None);

            workModule?.NotifyCompleted(_player, particlePos);
            foodCtx?.ClearFood();

            ticket.TryStartProgress(_player);
            _orderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, _player));
        }
    }
}
