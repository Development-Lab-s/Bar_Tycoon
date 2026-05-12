using BBJ.Data;
using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Schedule;
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Customer
{
    public class CustomerAgent : Customer
    {
        [SerializeField] private EventChannelSO _scheduleTriggerChannel;
        [SerializeField] private EventChannelSO _orderChannel;

        public FoodDataSO  SelectedFood    { get; private set; }
        public bool        IsAwaitingOrder { get; private set; }
        public Workplace   AssignedSeat    { get; set; }
        public OrderTicket ActiveTicket    { get; private set; }
        public bool        OrderPlaced     { get; private set; }
        public bool        FoodServed      { get; private set; }
        public bool        PaymentDone     { get; private set; }

        public bool IsReadyForOrder => IsAwaitingOrder;

        public void StartCycle(FoodDataSO food)
        {
            SelectedFood = food;
            _scheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());
        }

        public void SetAwaitingOrder(bool value) => IsAwaitingOrder = value;

        public OrderTicket PlaceOrder(Workplace seat)
        {
            if (OrderPlaced) return null;
            ActiveTicket    = new OrderTicket(SelectedFood, this, seat);
            OrderPlaced     = true;
            IsAwaitingOrder = false;
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
            GetModule<SchedulingModule>()?.CompleteWork();
            AssignedSeat    = null;
            ActiveTicket    = null;
            SelectedFood    = null;
            OrderPlaced     = false;
            FoodServed      = false;
            PaymentDone     = false;
            IsAwaitingOrder = false;
        }
    }
}
