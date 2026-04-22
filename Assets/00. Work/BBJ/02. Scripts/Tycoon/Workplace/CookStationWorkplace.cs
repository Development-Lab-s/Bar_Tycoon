using UnityEngine;

namespace BBJ.Tycoon
{
    public class CookStationWorkplace : Workplace
    {
        public OrderTicket AssignedTicket { get; private set; }

        public void AssignTicket(OrderTicket ticket)
        {
            AssignedTicket = ticket;
            Occupy();
        }

        public void ClearTicket()
        {
            AssignedTicket = null;
            Release();
        }
    }
}
