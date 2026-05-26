using System;
using System.Collections.Generic;
using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Player;
using BBJ.Schedule;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Assets._00._Work.PCM._02._Scripts.Contract;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Lusaload._02._Scripts.SO;

namespace BBJ.Customer
{
    public class CustomerAgent : Customer
    {
        [SerializeField] private EventChannelSO _scheduleChannel;
        [SerializeField] private EventChannelSO _orderChannel;

        public CocktailRecipeSO SelectedFood { get; private set; }
        public bool IsAwaitingOrder { get; private set; }
        public Workplace AssignedSeat { get; set; }
        public OrderTicket ActiveTicket { get; private set; }
        public bool OrderPlaced { get; private set; }
        public bool FoodServed { get; private set; }
        public bool PaymentDone { get; private set; }
        public ModuleOwner AssignedServer { get; private set; }

        [SerializeField] private PlayerOrderHandlerSO _playerOrderHandler;
        [SerializeField] private ChatSO _chatLines;

        public PlayerOrderHandlerSO PlayerOrderHandler => _playerOrderHandler;
        public ChatSO ChatLines => _chatLines;

        public int RestoreCycleStep { get; set; }

        public bool IsReadyForOrder => IsAwaitingOrder;

        public event Action OnOrderStateChanged;

        public void StartCycle(CocktailRecipeSO food)
        {
            SelectedFood = food;
            _orderChannel?.AddListener<OrderStateChangedEvent>(HandleOrderStateChanged);
            ChangeState(CustomerState.Idle);
            _scheduleChannel?.RaiseEvent(new ScheduleTriggerEvent());
        }

        public void SetAwaitingOrder(bool value)
        {
            IsAwaitingOrder = value;
            OnOrderStateChanged?.Invoke();
        }

        private void HandleOrderStateChanged(OrderStateChangedEvent e)
        {
            if (e.Ticket == ActiveTicket)
                OnOrderStateChanged?.Invoke();
        }

        public void SetAssignedServer(ModuleOwner server) => AssignedServer = server;

        public OrderTicket PlaceOrder(Workplace seat)
        {
            if (OrderPlaced) return null;
            ActiveTicket = new OrderTicket(SelectedFood, this, seat);
            OrderPlaced  = true;
            OnOrderStateChanged?.Invoke();
            return ActiveTicket;
        }

        public void OnFoodServed()
        {
            if (!OrderPlaced || FoodServed) return;
            FoodServed = true;
            OnOrderStateChanged?.Invoke();
        }

        public void OnPaymentDone()
        {
            if (!FoodServed || PaymentDone) return;
            if (ActiveTicket != null)
                _orderChannel?.RaiseEvent(new OrderFinishedEvent { Ticket = ActiveTicket });
            PaymentDone = true;
        }

        internal void RestoreActiveTicket(OrderTicket ticket)
        {
            SelectedFood = ticket.Ordered;
            ActiveTicket = ticket;
            OrderPlaced  = true;
            _orderChannel?.AddListener<OrderStateChangedEvent>(HandleOrderStateChanged);
        }

        internal void SetFoodServedForRestore() => FoodServed = true;

        public override void ResetItem()
        {
            _orderChannel?.RemoveListener<OrderStateChangedEvent>(HandleOrderStateChanged);

            // 활성 티켓이 있으면 OrderManager 흐름을 통해 취소 → CTS 전파로 진행 중인 워커도 중단
            if (ActiveTicket != null && !ActiveTicket.IsTerminal)
                _orderChannel?.RaiseEvent(new OrderCancelRequestEvent(ActiveTicket, CancelReason.CustomerLeft));

            GetModule<SchedulingModule>()?.CancelWork();
            var seat = AssignedSeat;
            if (seat != null)
            {
                seat.GetModule<SeatModule>()?.ClearCustomer();
                seat.GetModule<OccupancyModule>()?.Release();
                AssignedSeat = null;
            }
            AssignedServer  = null;
            ActiveTicket    = null;
            SelectedFood    = null;
            OrderPlaced     = false;
            FoodServed      = false;
            PaymentDone     = false;
            IsAwaitingOrder = false;
            RestoreCycleStep = 0;
        }
    }
}
