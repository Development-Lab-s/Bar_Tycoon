using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.UI;
using Cysharp.Threading.Tasks;

namespace BBJ.States
{
    public class CustomerWorkState : CustomerAgentState
    {
        private readonly WorkAction    _workAction;
        private readonly IAgentUIModule _uiModule;
        private readonly EatDurationUI  _eatDurationUI;

        private bool _workEnded;

        public CustomerWorkState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _workAction    = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();
            _uiModule      = owner.GetModule<IAgentUIModule>();
            _eatDurationUI = _uiModule?.Get<EatDurationUI>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _workEnded, CustomerState.Idle);
        }

        public override void Enter()
        {
            base.Enter();
            _workEnded = false;
            _workAction.OnWorkPhaseEnded  += HandleWorkEnded;
            _workAction.OnProgressUpdated += HandleProgress;

            _ = _eatDurationUI?.OpenAsync();
        }

        public override void Exit()
        {
            _workAction.OnWorkPhaseEnded  -= HandleWorkEnded;
            _workAction.OnProgressUpdated -= HandleProgress;

            _ = _eatDurationUI?.CloseAsync();
        }

        private void HandleWorkEnded()           => _workEnded = true;
        private void HandleProgress(float value) => _eatDurationUI?.SetPercent(value);
    }
}
