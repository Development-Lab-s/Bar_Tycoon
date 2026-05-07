using BBJ.Actions;
using BBJ.Staff.FSM;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    public class AgentActionWorkSO : WorkSO
    {
        [SerializeField] private TycoonAgentAction action;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)
        {
            var agent = executor as IActionDispatcher;
            if (agent == null) return;
            await agent.ExecuteStateAsync(action, context, ct);
        }
    }
}
