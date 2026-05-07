using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Customer;
using BBJ.Data;
using BBJ.WorkplaceSystem;
using System;

namespace BBJ.Order
{
    public class OrderTicket
    {
        public readonly FoodDataSO    Food;
        public ModuleOwner Customer { get; }
        public Workplace Seat { get; }

        public OrderState State { get; private set; } = OrderState.Waiting;

        public event Action<OrderTicket, OrderState> OnStateChanged;

        public OrderTicket(FoodDataSO food, ModuleOwner customer, Workplace seat)
        {
            Food     = food;
            Customer = customer;
            Seat     = seat;
        }

        public void ChangeState(OrderState next)
        {
            if (State == next) return;
            State = next;
            OnStateChanged?.Invoke(this, next);
        }
    }
}
