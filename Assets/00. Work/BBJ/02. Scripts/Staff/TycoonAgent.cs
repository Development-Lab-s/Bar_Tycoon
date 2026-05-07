using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using BBJ.Actions;
using BBJ.GridSystem.Pathfind;
using BBJ.Staff.FSM;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace BBJ
{
    public class TycoonAgent : Agent, IActionDispatcher, IAgentPathProvider
    {
        [SerializeField] private StateListSO stateList;
        [SerializeField] private RuntimeReference<IPathRequestManager> _pathRequest;
        private AgentStateMachine _stateMachine;
        public RuntimeReference<IPathRequestManager> PathRequest => _pathRequest;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            _stateMachine = new AgentStateMachine(this, stateList.states);
        }

        public void ChangeState(TycoonAgentAction newStateIndex)
            => _stateMachine.ChangeState((int)newStateIndex);

        public IEnumerator ExecuteState(TycoonAgentAction newStateIndex, GameEvent gameEvent)
        {
            ChangeState(newStateIndex);
            var action = _stateMachine.CurrentState as AgentActionBase;
            yield return StartCoroutine(action.Execute(gameEvent));
        }

        public UniTask ExecuteStateAsync(TycoonAgentAction newStateIndex, GameEvent gameEvent, CancellationToken ct)
        {
            ChangeState(newStateIndex);
            var action = _stateMachine.CurrentState as AgentActionBase;
            return action != null ? action.ExecuteAsync(gameEvent, ct) : UniTask.CompletedTask;
        }

        public void ExecuteIdle() => ExecuteState(TycoonAgentAction.Idle, new GameEvent());
    }
}
