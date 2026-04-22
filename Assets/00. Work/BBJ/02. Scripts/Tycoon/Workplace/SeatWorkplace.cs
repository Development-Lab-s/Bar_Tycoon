using UnityEngine;

namespace BBJ.Tycoon
{
    public class SeatWorkplace : Workplace
    {
        public CustomerAgent AssignedCustomer { get; private set; }

        public bool IsWaitingForOrder =>
            AssignedCustomer != null &&
            AssignedCustomer.CurrentPhase == CustomerPhase.WaitingOrder;

        public void AssignCustomer(CustomerAgent customer)
        {
            AssignedCustomer = customer;
            Occupy();
        }

        public void ClearCustomer()
        {
            AssignedCustomer = null;
            Release();
        }
    }
}
