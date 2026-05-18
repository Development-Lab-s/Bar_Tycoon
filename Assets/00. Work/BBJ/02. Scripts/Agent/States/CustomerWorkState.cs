using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;

namespace BBJ.States
{
    public class CustomerWorkState : TransitionAgentState
    {
        private readonly WorkAction _workAction;

        private bool _workEnded;

        public CustomerWorkState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _workAction = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _workEnded, CustomerState.Idle);
        }

        public override void Enter()
        {
            base.Enter();
            _workEnded = false;
            _workAction.OnWorkPhaseEnded += HandleWorkEnded;
        }

        public override void Exit()
        {
            _workAction.OnWorkPhaseEnded -= HandleWorkEnded;
        }

        private void HandleWorkEnded() => _workEnded = true;
    }
}
