using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using Agents.StatSystem;
using BBJ.Actions;
using BBJ.GridSystem.Pathfind;
using BBJ.Movement;
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
        [SerializeField] private StatSO _speedStat;

        private AgentStateMachine _stateMachine;
        public IStatModule Stat { get; private set; }
        public IPathMovement Movement { get; private set; }

        public RuntimeReference<IPathRequestManager> PathRequest => _pathRequest;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            _stateMachine = new AgentStateMachine(this, stateList.states);

            Stat = GetModule<IStatModule>();
            Movement = GetModule<IPathMovement>();
        }

        protected override void AfterInitComponents()
        {
            base.AfterInitComponents();
            float speed = Stat.SubscribeStat(_speedStat.AssetIndex, OnSpeedStatChanged, 1f);
            Movement.OnSpeedChanged(speed);
        }

        private void OnDestroy()
        {
            Stat?.UnSubscribeStat(_speedStat.AssetIndex, OnSpeedStatChanged);
        }
        
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
        public void ChangeState(TycoonAgentAction newStateIndex) => _stateMachine.ChangeState((int)newStateIndex);
        private void OnSpeedStatChanged(StatSO stat, float current, float _) => Movement?.OnSpeedChanged(current);

        public void SetMoveDestination(Vector3 destination)
        {
            _pathRequest.Instance.RequestPath(transform.position, destination,
                (path, success) =>
                {
                    if (success && path.Length > 0) Movement.OnPathMove(path);
                    else Movement.OnPathMove(System.Array.Empty<Vector3>());
                });
        }
    }
}
