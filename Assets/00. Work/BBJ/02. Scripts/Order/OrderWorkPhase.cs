namespace BBJ.Order
{
    public enum OrderWorkPhase
    {
        PendingCook,      // waiting to be cooked (or currently cooking — check State)
        ReadyForServe,    // waiting to be served (or currently serving — check State)
        ReadyForCashier,  // waiting for cashier to process payment
        Done,
        ReadyForServer,   // customer waiting for server to take order
    }
}
