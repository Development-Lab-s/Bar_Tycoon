using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.Movement;
using BBJ.Schedule;
using BBJ.UI;
using UnityEngine;

namespace BBJ.States
{
    public class CustomerIdleState : TransitionAgentState
    {
        private readonly IPathMovement  _movement;
        private readonly ISchedulable   _scheduling;
        private readonly IAgentUIModule _uiModule;
        private readonly WorkAction     _workAction;
        private readonly CustomerAgent  _customer;
        private readonly AgentStatusUI  _statusUI;

        private bool _isMoveStarted;
        private bool _shouldWork;

        public CustomerIdleState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _customer = owner as CustomerAgent;

            _movement   = owner.GetModule<IPathMovement>();
            _scheduling = owner.GetModule<ISchedulable>();
            _uiModule   = owner.GetModule<IAgentUIModule>();
            _workAction = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();
            _statusUI   = _uiModule.Get<AgentStatusUI>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _isMoveStarted, CustomerState.Move);
            AddTransitionToEnum(() => _shouldWork,    CustomerState.Work);
        }

        public override void Enter()
        {
            base.Enter();
            _isMoveStarted = false;
            _shouldWork    = false;

            if (IsWorking()) { HandleWorkPhaseStarted(); return; }
            if (IsMoving())  { HandleMoveStarted();      return; }

            RefreshUI();

            _movement.OnMoveStarted        += HandleMoveStarted;
            _workAction.OnWorkPhaseStarted += HandleWorkPhaseStarted;
            _customer.OnOrderStateChanged  += RefreshUI;
        }

        public override void Exit()
        {
            base.Exit();
            _uiModule.SetActiveUI<AgentStatusUI>(false);

            _movement.OnMoveStarted        -= HandleMoveStarted;
            _workAction.OnWorkPhaseStarted -= HandleWorkPhaseStarted;
            _customer.OnOrderStateChanged  -= RefreshUI;
        }

        private void HandleMoveStarted()      => _isMoveStarted = true;
        private void HandleWorkPhaseStarted() => _shouldWork = true;
        private bool IsMoving()  => _movement != null && _movement.IsMoving;
        private bool IsWorking() => _scheduling != null && !_scheduling.IsAvailableForWork
                                    && _workAction != null && _workAction.IsInWorkPhase;

        private void RefreshUI()
        {
            if (_customer.FoodServed)
            {
                _uiModule.SetActiveUI<AgentStatusUI>(false);
                return;
            }

            _uiModule.SetActiveUI<AgentStatusUI>(true);

            if (!_customer.OrderPlaced || _customer.IsAwaitingOrder)
            {
                _statusUI.SetText("...");
                return;
            }

            var ticket = _customer.ActiveTicket;
            var icon   = _customer.SelectedFood?.cocktailIcon;
            if (icon != null)
            {
                _statusUI.SetIcon(icon);
                _statusUI.SetIconColor(ticket?.IsPlayerActionable ?? false ? Color.white : Color.gray);
            }
            else
            {
                _statusUI.SetText(_customer.SelectedFood?.cocktailName ?? "...");
            }
        }
    }
}
