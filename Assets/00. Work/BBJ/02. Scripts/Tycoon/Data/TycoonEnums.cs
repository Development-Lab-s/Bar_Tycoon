namespace BBJ.Tycoon.Data
{
    public enum WorkplaceType
    {
        None,
        Seat,
        Counter,
        CookStation,
        OrderPoint,
    }

    public enum OrderState
    {
        Waiting,
        Ordered,
        Cooking,
        Ready,
        Served,
        Paying,
        Done,
    }
}
