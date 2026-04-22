using System;

namespace BBJ.Tycoon
{

    public class OrderTicket
    {
        public readonly int           TicketId;
        public readonly FoodDataSO    Food;
        public readonly CustomerAgent Customer;
        public readonly SeatWorkplace Seat;

        public OrderState State { get; private set; } = OrderState.Waiting;

        public event Action<OrderTicket, OrderState> OnStateChanged;

        private static int _idCounter;

        public static void ResetCounter() => _idCounter = 0;

        public OrderTicket(FoodDataSO food, CustomerAgent customer, SeatWorkplace seat)
        {
            TicketId = ++_idCounter;
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
