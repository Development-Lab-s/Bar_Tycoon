using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Movement;
using BBJ.Modules;
using BBJ.Schedule;

namespace BBJ.States
{
    public class StaffIdleState : TransitionAgentState
    {
        private readonly IPathMovement  _movement;
        private readonly ISchedulable   _scheduling;
        private readonly WorkAction     _workAction;
        private readonly IAgentInput    _input;

        private bool _isMoveStarted;
        private bool _shouldWork;
        private bool _shouldInteract;

        public StaffIdleState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _movement   = owner.GetModule<IPathMovement>();
            _scheduling = owner.GetModule<ISchedulable>();
            _workAction = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();
            _input      = owner.GetModule<IAgentInput>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _isMoveStarted,  StaffState.Move);
            AddTransitionToEnum(() => _shouldWork,     StaffState.Work);
            AddTransitionToEnum(() => _shouldInteract, StaffState.Interact);
        }

        public override void Enter()
        {
            base.Enter();
            _isMoveStarted  = false;
            _shouldWork     = false;
            _shouldInteract = false;

            if (IsWorking()) { HandleWorkPhaseStarted(); return; }
            if (IsMoving())  { HandleMoveStarted();      return; }

            _movement.OnMoveStarted        += HandleMoveStarted;
            _workAction.OnWorkPhaseStarted += HandleWorkPhaseStarted;
            _input.OnInteracted += HandleInteract;
        }

        public override void Exit()
        {
            base.Exit();
            _movement.OnMoveStarted        -= HandleMoveStarted;
            _workAction.OnWorkPhaseStarted -= HandleWorkPhaseStarted;
            _input.OnInteracted            -= HandleInteract;
        }

        private void HandleMoveStarted()      => _isMoveStarted = true;
        private void HandleWorkPhaseStarted() => _shouldWork = true;
        private void HandleInteract()         => _shouldInteract = true;
        private bool IsMoving()  => _movement != null && _movement.IsMoving;
        private bool IsWorking() => _scheduling != null && !_scheduling.IsAvailableForWork && _workAction != null && _workAction.IsInWorkPhase;
    }
}
