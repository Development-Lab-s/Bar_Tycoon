using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using BBJ.Agents.FSM;
using BBJ.Modules;
using BBJ.Tycoon;
using UnityEngine;

namespace BBJ.Agents
{
    public class WorkerAgent : Agent
    {
        [SerializeField] private StateListSO    _stateList;
        [SerializeField] private WorkerConfigSO _config;

        public WorkerConfigSO Config => _config;

        private AgentStateMachine _stateMachine;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();

            Debug.Assert(_config    != null, $"[WorkerAgent] {gameObject.name}: WorkerConfigSO 미할당");
            Debug.Assert(_stateList != null, $"[WorkerAgent] {gameObject.name}: StateListSO 미할당");

            _stateMachine = new AgentStateMachine(this, _stateList.states);

            // FSM 전환 요청을 WorkerAgent가 단독으로 처리
            var scheduling = GetModule<SchedulingModule>();
            if (scheduling != null)
                scheduling.OnStateChangeRequested += OnStateChangeRequested;
        }

        private void OnDestroy()
        {
            var scheduling = GetModule<SchedulingModule>();
            if (scheduling != null)
                scheduling.OnStateChangeRequested -= OnStateChangeRequested;
        }

        private void OnStateChangeRequested(WorkerState state)
            => _stateMachine.ChangeState((int)state);

        private void Update() => _stateMachine?.UpdateMachine();
    }
}
