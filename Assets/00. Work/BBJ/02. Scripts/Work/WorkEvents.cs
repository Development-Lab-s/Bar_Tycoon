using BBJ.Order;
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;

namespace BBJ.Work
{
    public class TakeOrderEvent : GameEvent
    {
        public Workplace Seat { get; }
        public TakeOrderEvent(Workplace seat) => Seat = seat;
    }

    public class OrderWorkEvent : GameEvent
    {
        public OrderTicket  Ticket       { get; }
        public OrderManager OrderManager { get; }

        public OrderWorkEvent(OrderTicket ticket, OrderManager orderManager)
        {
            Ticket       = ticket;
            OrderManager = orderManager;
        }
    }

}
