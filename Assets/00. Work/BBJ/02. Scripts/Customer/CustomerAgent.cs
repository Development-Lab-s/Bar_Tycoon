using BBJ.Data;
using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Customer
{
    public class CustomerAgent : Customer
    {
        [SerializeField] private EventChannelSO _scheduleChannel;
        [SerializeField] private EventChannelSO _orderChannel;

        public FoodDataSO SelectedFood { get; private set; }
        public bool IsAwaitingOrder { get; private set; }
        public Workplace AssignedSeat { get; set; }
        public OrderTicket ActiveTicket { get; private set; }
        public bool OrderPlaced { get; private set; }
        public bool FoodServed { get; private set; }
        public bool PaymentDone { get; private set; }
        public ModuleOwner AssignedServer { get; private set; }

        public bool IsReadyForOrder => IsAwaitingOrder;

        public void StartCycle(FoodDataSO food)
        {
            SelectedFood = food;
            _scheduleChannel?.RaiseEvent(new ScheduleTriggerEvent());
        }

        public void SetAwaitingOrder(bool value) => IsAwaitingOrder = value;

        public void SetAssignedServer(ModuleOwner server) => AssignedServer = server;

        public OrderTicket PlaceOrder(Workplace seat)
        {
            if (OrderPlaced) return null;
            ActiveTicket = new OrderTicket(SelectedFood, this, seat);
            OrderPlaced  = true;
            return ActiveTicket;
        }

        public void OnFoodServed()
        {
            if (!OrderPlaced || FoodServed) return;
            FoodServed = true;
        }

        public void OnPaymentDone()
        {
            if (!FoodServed || PaymentDone) return;
            if (ActiveTicket != null)
                _orderChannel?.RaiseEvent(new OrderFinishedEvent { Ticket = ActiveTicket });
            PaymentDone = true;
        }

        public override void ResetItem()
        {
            // 활성 티켓이 있으면 OrderManager 흐름을 통해 취소 → CTS 전파로 진행 중인 워커도 중단
            if (ActiveTicket != null && !ActiveTicket.IsTerminal)
                _orderChannel?.RaiseEvent(new OrderCancelRequestEvent(ActiveTicket, CancelReason.CustomerLeft));

            GetModule<SchedulingModule>()?.CancelWork();
            AssignedSeat    = null;
            AssignedServer  = null;
            ActiveTicket    = null;
            SelectedFood    = null;
            OrderPlaced     = false;
            FoodServed      = false;
            PaymentDone     = false;
            IsAwaitingOrder = false;
        }
    }
}
