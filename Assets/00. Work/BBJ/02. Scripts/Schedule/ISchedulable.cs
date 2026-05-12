using BBJ.Staff;
using BBJ.Work;
using Gamelib.EventSystem;
using System;

namespace BBJ.Schedule
{
    public interface ISchedulable
    {
        bool IsAvailableForWork { get; }
        AgentRole Role          { get; }

        event Action OnWorkStarted;
        event Action OnWorkEnded;

        void AssignWork(WorkSO workSO, GameEvent context);
        void CompleteWork();
    }

    public interface IScheduleTriggerSource
    {
        Gamelib.EventSystem.EventChannelSO ScheduleTriggerChannel { get; }
    }
}
