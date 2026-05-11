namespace BBJ.Order
{
    public enum OrderState
    {
        Waiting,     // awaiting dispatch
        Reserved,    // claimed by a worker, not yet executing
        InProgress,  // actively executing
        Done,        // terminal: completed
        Cancelled    // terminal: cancelled with reason
    }
}
