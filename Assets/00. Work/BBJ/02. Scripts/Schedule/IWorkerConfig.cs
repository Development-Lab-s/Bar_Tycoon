using BBJ.Tycoon;
using System.Collections.Generic;

namespace BBJ.Schedule
{
    public interface IWorkerConfig
    {
        IReadOnlyList<WorkSO> PriorityWorks { get; }
    }
}
