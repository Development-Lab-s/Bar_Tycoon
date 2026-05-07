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

    public class CookEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public CookEvent(OrderTicket ticket) => Ticket = ticket;
    }

    public class ServeEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public Workplace   Seat   { get; }
        public ServeEvent(OrderTicket ticket, Workplace seat) { Ticket = ticket; Seat = seat; }
    }

    public class CashierEvent : GameEvent
    {
        public Workplace Counter { get; }
        public CashierEvent(Workplace counter) => Counter = counter;
    }
}
