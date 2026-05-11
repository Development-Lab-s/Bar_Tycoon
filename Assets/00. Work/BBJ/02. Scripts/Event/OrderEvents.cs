using BBJ.Order;
using Gamelib.EventSystem;

namespace BBJ.EventSystem
{
    public class OrderRegisteredEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public OrderRegisteredEvent(OrderTicket ticket) => Ticket = ticket;
    }

    public class OrderUnregisteredEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public OrderUnregisteredEvent(OrderTicket ticket) => Ticket = ticket;
    }

    public class OrderStateChangedEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public OrderStateChangedEvent(OrderTicket ticket) => Ticket = ticket;
    }
}