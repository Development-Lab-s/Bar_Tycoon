using BBJ.Order;
using BBJ.Staff;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    public abstract class WorkSO : ScriptableObject
    {
        public AgentRole RequiredRole;

        [SerializeField] protected WorkContextSO _ctx;

        public UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
            => RunAsync(executor, ticket, ctx);

        protected abstract UniTask<WorkResult> RunAsync(
            ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx);
    }
}
