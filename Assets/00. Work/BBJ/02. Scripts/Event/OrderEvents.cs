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

    public class OrderTicketRegisterEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public OrderTicketRegisterEvent(OrderTicket ticket) => Ticket = ticket;
    }

    public class OrderReadyForServerEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public OrderReadyForServerEvent(OrderTicket ticket) => Ticket = ticket;
    }

    public class OrderCancelRequestEvent : GameEvent
    {
        public OrderTicket  Ticket { get; }
        public CancelReason Reason { get; }

        public OrderCancelRequestEvent(OrderTicket ticket, CancelReason reason)
        {
            Ticket = ticket;
            Reason = reason;
        }
    }

    public class PlayerOrderTakeEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public PlayerOrderTakeEvent(OrderTicket ticket) => Ticket = ticket;
    }

    public class CustomerEatCompleteEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public CustomerEatCompleteEvent(OrderTicket ticket) => Ticket = ticket;
    }

    public class PlayerCraftStartEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public PlayerCraftStartEvent(OrderTicket ticket) => Ticket = ticket;
    }
}