using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Order;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Modules
{
    public class TicketSlotModule : MonoBehaviour, IModule
    {
        public OrderTicket AssignedTicket { get; private set; }

        public void Initialize(ModuleOwner owner) { }

        public void AssignTicket(OrderTicket ticket) => AssignedTicket = ticket;
        public void ClearTicket()                    => AssignedTicket = null;
    }
}
