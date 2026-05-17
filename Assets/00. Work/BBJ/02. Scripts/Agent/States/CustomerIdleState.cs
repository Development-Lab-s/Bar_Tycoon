using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.Movement;
using BBJ.Schedule;

namespace BBJ.States
{
    public class CustomerIdleState : TransitionAgentState
    {
        private readonly IPathMovement _movement;
        private readonly ISchedulable _scheduling;
        private readonly IAgentUIModule _uiModule;
        private readonly WorkAction _workAction;
        private readonly CustomerAgent _customer;

        private bool _isMoveStarted;
        private bool _shouldWork;

        public CustomerIdleState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _customer = owner as CustomerAgent;

            _movement   = owner.GetModule<IPathMovement>();
            _scheduling = owner.GetModule<ISchedulable>();
            _uiModule   = owner.GetModule<IAgentUIModule>();
            _workAction = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _isMoveStarted, CustomerState.Move);
            AddTransitionToEnum(() => _shouldWork, CustomerState.Work);
        }

        public override void Enter()
        {
            base.Enter();
            _isMoveStarted = false;
            _shouldWork = false;

            if (IsWorking()) { HandleWorkPhaseStarted(); return; }
            if (IsMoving())  { HandleMoveStarted();      return; }

            _uiModule.SetActiveUI<CustomerWaitUI>(true);
            _uiModule.Get<CustomerWaitUI>()?.Refresh(_customer);

            _movement.OnMoveStarted        += HandleMoveStarted;
            _workAction.OnWorkPhaseStarted += HandleWorkPhaseStarted;
        }

        public override void Exit()
        {
            base.Exit();
            _uiModule.SetActiveUI<CustomerWaitUI>(false);

            _movement.OnMoveStarted        -= HandleMoveStarted;
            _workAction.OnWorkPhaseStarted -= HandleWorkPhaseStarted;
        }
        private void HandleMoveStarted()      => _isMoveStarted = true;
        private void HandleWorkPhaseStarted() => _shouldWork = true;
        private bool IsMoving()  => _movement != null && _movement.IsMoving;
        private bool IsWorking() => _scheduling != null && !_scheduling.IsAvailableForWork && _workAction != null && _workAction.IsInWorkPhase;
    }
}
