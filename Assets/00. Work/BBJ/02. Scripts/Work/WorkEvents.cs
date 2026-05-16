using BBJ.Order;
using BBJ.Staff;
using Gamelib.EventSystem;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    public class OrderNotifyCompleteEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public ModuleOwner Actor  { get; }
        public OrderNotifyCompleteEvent(OrderTicket ticket, ModuleOwner actor)
        {
            Ticket = ticket;
            Actor  = actor;
        }
    }

    public class OrderNotifyReleasedEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public ModuleOwner Actor  { get; }
        public OrderNotifyReleasedEvent(OrderTicket ticket, ModuleOwner actor)
        {
            Ticket = ticket;
            Actor  = actor;
        }
    }

    public class CookingStartEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public ModuleOwner Staff  { get; }

        public CookingStartEvent(OrderTicket ticket, ModuleOwner staff)
        {
            Ticket = ticket;
            Staff  = staff;
        }
    }

    public class ScheduleRequestEvent : GameEvent
    {
        public AgentRole   Role   { get; }
        public WorkSO      Work   { get; }
        public OrderTicket Ticket { get; }

        public ScheduleRequestEvent(AgentRole role, WorkSO work, OrderTicket ticket)
        {
            Role   = role;
            Work   = work;
            Ticket = ticket;
        }
    }
}
