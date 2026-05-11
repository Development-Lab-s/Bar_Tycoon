using BBJ.Data;
using BBJ.WorkplaceSystem;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Order
{
    public class OrderTicket
    {
        public FoodDataSO  Food      { get; }
        public ModuleOwner Customer  { get; }
        public Workplace   Seat      { get; }

        public OrderState     State              { get; private set; } = OrderState.Waiting;
        public OrderWorkPhase WorkPhase          { get; internal set; } = OrderWorkPhase.PendingCook;
        public ModuleOwner    ReservedBy         { get; private set; }
        public CancelReason?  CancellationReason { get; private set; }

        public OrderTicket(FoodDataSO food, ModuleOwner customer, Workplace seat)
        {
            Food     = food;
            Customer = customer;
            Seat     = seat;
        }

        internal bool Advance()
        {
            if (State >= OrderState.InProgress) return false;
            State++;
            return true;
        }

        public bool TryReserve(ModuleOwner actor)
        {
            if (actor == null || State != OrderState.Waiting) return false;
            ReservedBy = actor;
            return Advance();
        }

        public bool TryStartProgress(ModuleOwner actor)
        {
            if (State != OrderState.Reserved || ReservedBy != actor) return false;
            return Advance();
        }

        internal void Release()
        {
            ReservedBy = null;
            State = OrderState.Waiting;
        }

        internal void Finish()
        {
            ReservedBy = null;
            State = OrderState.Done;
        }

        internal void Cancel(CancelReason reason)
        {
            CancellationReason = reason;
            ReservedBy = null;
            State = OrderState.Cancelled;
        }
    }
}
