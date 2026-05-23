using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using Assets._00._Work.PCM._02._Scripts.Contract;
using BBJ.Modules;
using BBJ.Schedule;
using BBJ.UI;

namespace BBJ.States
{
    public class InteractState : TransitionAgentState
    {
        private readonly SchedulingModule _scheduling;
        private readonly IAgentUIModule   _uiModule;

        public InteractState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _scheduling = owner.GetModule<SchedulingModule>();
            _uiModule   = owner.GetModule<IAgentUIModule>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _isTriggerCall, StaffState.Idle);
        }

        public override void Enter()
        {
            base.Enter();
            _uiModule.SetActiveUI<ContractChat>(true);
        }

        public override void Exit()
        {
            base.Exit();
            _uiModule.SetActiveUI<ContractChat>(false);
            _scheduling?.Resume();
        }
    }
}
