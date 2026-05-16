using BBJ.Order;
using BBJ.Staff;
using BBJ.Work;
using System;

namespace BBJ.Schedule
{
    public interface ISchedulable
    {
        bool IsAvailableForWork { get; }
        AgentRole Role          { get; }

        event Action OnWorkStarted;
        event Action<bool> OnWorkEnded;
        void AssignWork(WorkSO workSO, OrderTicket ticket);
        void CancelWork();
    }
}
