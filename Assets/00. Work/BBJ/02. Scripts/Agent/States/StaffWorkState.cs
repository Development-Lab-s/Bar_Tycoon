using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Modules;
using BBJ.UI;
using Cysharp.Threading.Tasks;

namespace BBJ.States
{
    public class StaffWorkState : TransitionAgentState
    {
        private readonly WorkAction     _workAction;
        private readonly IAgentInput    _input;
        private readonly IAgentUIModule _uiModule;
        private readonly AgentStatusUI  _statusUI;

        private bool _workEnded;
        private bool _shouldInteract;

        public StaffWorkState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _workAction = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();
            _input      = owner.GetModule<IAgentInput>();
            _uiModule   = owner.GetModule<IAgentUIModule>();
            _statusUI   = _uiModule.Get<AgentStatusUI>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _workEnded,      StaffState.Idle);
            AddTransitionToEnum(() => _shouldInteract, StaffState.Interact);
        }

        public override void Enter()
        {
            base.Enter();
            _workEnded      = false;
            _shouldInteract = false;

            _statusUI?.ToggleText("작업중");
            _statusUI?.OpenAsync().Forget();
            _workAction.OnWorkPhaseEnded += HandleWorkEnded;
            _input.OnInteracted          += HandleInteract;
        }

        public override void Exit()
        {
            _statusUI?.CloseAsync().Forget();
            _workAction.OnWorkPhaseEnded -= HandleWorkEnded;
            _input.OnInteracted          -= HandleInteract;
        }

        private void HandleWorkEnded() => _workEnded = true;
        private void HandleInteract()  => _shouldInteract = true;
    }
}
