using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Collections;
using System.Threading;

namespace BBJ.Actions
{
    public abstract class AgentActionBase : AgentState, IAgentAction
    {
        protected AgentActionBase(Agent owner, AnimParamSO stateParam)
            : base(owner, stateParam) { }

        public abstract IEnumerator Execute(GameEvent param);
        public abstract UniTask ExecuteAsync(GameEvent param, CancellationToken ct);
    }
}
