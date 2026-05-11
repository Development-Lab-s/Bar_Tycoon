using System;

namespace BBJ.Staff
{
    [Flags]
    public enum AgentRole
    {
        None     = 0,
        Customer = 1 << 0,
        Server   = 1 << 1,
        Cooker   = 1 << 2,
        Cashier  = 1 << 3,
        Cleaner  = 1 << 4,
    }
}
