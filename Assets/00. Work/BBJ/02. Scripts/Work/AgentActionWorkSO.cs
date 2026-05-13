using BBJ.Actions;
using BBJ.Staff.FSM;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    public class AgentActionWorkSO : WorkSO
    {
        [SerializeField] private TycoonAgentAction action;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            if (agent == null) return WorkResult.Cancelled;
            try
            {
                await agent.ExecuteStateAsync(action, context, ctx.Token);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
