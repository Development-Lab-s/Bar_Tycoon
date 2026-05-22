using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.UI;

namespace BBJ.States
{
    public class CustomerWorkState : TransitionAgentState
    {
        private readonly WorkAction     _workAction;
        private readonly IAgentUIModule _uiModule;
        private readonly AgentStatusUI  _statusUI;

        private bool _workEnded;

        public CustomerWorkState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _workAction = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();
            _uiModule   = owner.GetModule<IAgentUIModule>();
            _statusUI   = _uiModule?.Get<AgentStatusUI>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _workEnded, CustomerState.Idle);
        }

        public override void Enter()
        {
            base.Enter();
            _workEnded = false;
            _workAction.OnWorkPhaseEnded += HandleWorkEnded;

            _statusUI?.SetText("먹는 중");
            _uiModule?.SetActiveUI<AgentStatusUI>(true);
        }

        public override void Exit()
        {
            _workAction.OnWorkPhaseEnded -= HandleWorkEnded;
            _uiModule?.SetActiveUI<AgentStatusUI>(false);
        }

        private void HandleWorkEnded() => _workEnded = true;
    }
}
