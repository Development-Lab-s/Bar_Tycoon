using UnityEngine;
using Agents.StatSystem;
using BBJ.Order;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem.Handlers
{
    public abstract class WorkCompletionHandlerSO : ScriptableObject
    {
        public abstract void OnCompleted(ModuleOwner executor, OrderTicket orderTicket );
    }
}
