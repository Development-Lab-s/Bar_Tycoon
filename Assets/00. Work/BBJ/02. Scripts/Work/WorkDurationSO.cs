using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using Agents.StatSystem;
using BBJ.Order;

namespace BBJ.Work
{
    public abstract class WorkDurationSO : ScriptableObject
    {
        public abstract float GetDuration(IStatModule workerStat, OrderTicket conductData);
    }
}
