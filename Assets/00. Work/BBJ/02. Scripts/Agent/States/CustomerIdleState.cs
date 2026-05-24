using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.Movement;
using BBJ.Schedule;
using BBJ.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace BBJ.States
{
    public class CustomerIdleState : CustomerAgentState
    {
        private readonly IPathMovement _movement;
        private readonly ISchedulable _scheduling;
        private readonly IAgentUIModule _uiModule;
        private readonly WorkAction _workAction;
        private readonly AgentStatusUI _statusUI;
        private readonly DialogueBubbleUI _dialogueUI;
        private readonly AgentBubbleUI _backUI;

        private bool _isMoveStarted;
        private bool _shouldWork;
        private CancellationTokenSource _uiCts;

        public CustomerIdleState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _movement = owner.GetModule<IPathMovement>();
            _scheduling = owner.GetModule<ISchedulable>();
            _uiModule = owner.GetModule<IAgentUIModule>();
            _workAction = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();
            _statusUI = _uiModule.Get<AgentStatusUI>();
            _dialogueUI = _uiModule.Get<DialogueBubbleUI>();
            _backUI = _uiModule.Get<AgentBubbleUI>();
            _uiCts = new CancellationTokenSource();

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
            if (IsMoving()) { HandleMoveStarted(); return; }

            _uiCts = new CancellationTokenSource();
            PlayOrderSequenceAsync().Forget();

            _movement.OnMoveStarted += HandleMoveStarted;
            _workAction.OnWorkPhaseStarted += HandleWorkPhaseStarted;
            _customer.OnOrderStateChanged += RefreshUI;
        }

        public override void Exit()
        {
            base.Exit();
            _uiCts?.Cancel();
            _uiCts?.Dispose();
            _uiCts = null;
            _uiModule.CloseAll();

            _movement.OnMoveStarted -= HandleMoveStarted;
            _workAction.OnWorkPhaseStarted -= HandleWorkPhaseStarted;
            _customer.OnOrderStateChanged -= RefreshUI;
        }

        private void HandleMoveStarted() => _isMoveStarted = true;
        private void HandleWorkPhaseStarted() => _shouldWork = true;
        private bool IsMoving() => _movement != null && _movement.IsMoving;
        private bool IsWorking() => _scheduling != null && !_scheduling.IsAvailableForWork
                                    && _workAction != null && _workAction.IsInWorkPhase;

        private async UniTaskVoid PlayOrderSequenceAsync()
        {
            if (_uiCts == null) return;
            var ct = _uiCts.Token;

            var line = "테스트";

            if (!string.IsNullOrEmpty(line) && _dialogueUI != null)
            {
                RefreshStatusUI();
                _dialogueUI.SetText(line);
                await _uiModule.PlaySequenceAsync(ct, _dialogueUI, _statusUI);
            }
            else
            {
                await _uiModule.PlaySequenceAsync(ct, _statusUI);
            }

            if (!ct.IsCancellationRequested)
                RefreshStatusUI();
        }

        private void RefreshUI() => RefreshStatusUI();

        private void RefreshStatusUI()
        {
            if (_statusUI == null) return;

            if (_customer.FoodServed)
            {
                _ = _statusUI.CloseAsync();
                return;
            }

            if (!_customer.OrderPlaced || _customer.IsAwaitingOrder)
            {
                _statusUI.ToggleText("...");
                return;
            }

            var ticket = _customer.ActiveTicket;
            Color alphaColor = _backUI.BackgroundImage.color;
            var newIcon = _customer.SelectedFood?.cocktailIcon;

            if (newIcon != null)
            {
                    _statusUI.ToggleIcon();
                if (!(_statusUI.Icon is IStylableUI icon)) return;

                if (ticket != null && ticket.IsPlayerActionable)
                {
                    // 완성 됨
                    icon.SetSprite(newIcon)
                        .SetRecolorFade(0f);

                    alphaColor.a = 1f;
                    _backUI.BackgroundImage.color = alphaColor;
                }
                else
                {
                    // 아직
                    icon.SetSprite(newIcon)
                        .SetRecolorFade(0.72f);

                    alphaColor.a = 148f / 255f;
                    _backUI.BackgroundImage.color = alphaColor;
                }
            }
            else
            {
                _statusUI.ToggleText(_customer.SelectedFood?.cocktailName ?? "...");
            }
        }
    }
}
