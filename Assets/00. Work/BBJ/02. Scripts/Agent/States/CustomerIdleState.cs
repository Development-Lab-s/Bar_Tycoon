using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using _00._Work.PCM._02._Scripts;
using Assets._00._Work.PCM._02._Scripts.Contract;
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
        private readonly IPathMovement   _movement;
        private readonly ISchedulable    _scheduling;
        private readonly IAgentUIModule  _uiModule;
        private readonly IHoverable      _hoverObj;
        private readonly IContractObject _contractObj;
        private readonly WorkAction      _workAction;
        private readonly AgentStatusUI   _statusUI;
        private readonly DialogueBubbleUI _dialogueUI;
        private readonly AgentBubbleUI   _backUI;

        private bool _isMoveStarted;
        private bool _shouldWork;
        private bool _initializing;
        private bool _isDialoguePlaying;
        private CancellationTokenSource _uiCts;

        public CustomerIdleState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _movement    = owner.GetModule<IPathMovement>();
            _scheduling  = owner.GetModule<ISchedulable>();
            _uiModule    = owner.GetModule<IAgentUIModule>();
            _hoverObj    = owner.GetModule<IHoverable>();
            _contractObj = owner.GetModule<IContractObject>();

            var actionModule = owner.GetModule<IAgentActionModule>();
            _workAction = actionModule.GetAction<WorkAction>();

            _statusUI   = _uiModule.Get<AgentStatusUI>();
            _dialogueUI = _uiModule.Get<DialogueBubbleUI>();
            _backUI     = owner.GetComponentInChildren<AgentBubbleUI>(true);

            _uiCts = new CancellationTokenSource();

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

            _contractObj?.OnClickEvent.AddListener(HandleClick);

            _uiCts = new CancellationTokenSource();
            PlayStatusSequenceAsync().Forget();

            _movement.OnMoveStarted              += HandleMoveStarted;
            _workAction.OnWorkPhaseStarted        += HandleWorkPhaseStarted;
            _customer.OnOrderStateChanged         += RefreshUI;
        }

        public override void Exit()
        {
            base.Exit();
            _uiCts?.Cancel();
            _uiCts?.Dispose();
            _uiCts = null;
            _uiModule.CloseAll();
            _ = _backUI?.CloseAsync();

            _contractObj?.OnClickEvent.RemoveListener(HandleClick);
            if (_contractObj != null) _contractObj.CanInteracting = false;

            _movement.OnMoveStarted              -= HandleMoveStarted;
            _workAction.OnWorkPhaseStarted        -= HandleWorkPhaseStarted;
            _customer.OnOrderStateChanged         -= RefreshUI;
        }

        private void HandleMoveStarted()      => _isMoveStarted = true;
        private void HandleWorkPhaseStarted() => _shouldWork = true;
        private bool IsMoving()  => _movement   != null && _movement.IsMoving;
        private bool IsWorking() => _scheduling != null && !_scheduling.IsAvailableForWork
                                 && _workAction  != null && _workAction.IsInWorkPhase;

        private void HandleClick()
        {
            var ticket = _customer.ActiveTicket;
            if (ticket != null && ticket.IsPlayerActionable)
                _customer.PlayerOrderHandler?.OnCustomerClicked(_customer);

            ShowDialogueSequence();
        }

        private void ShowDialogueSequence()
        {
            var lines = _customer.ChatLines;
            if (lines == null || lines.Line.Count == 0) return;

            _uiCts?.Cancel();
            _uiCts?.Dispose();
            _uiCts = new CancellationTokenSource();

            var line = lines.Line[Random.Range(0, lines.Line.Count)];
            RunDialogueAsync(_uiCts.Token, line).Forget();
        }

        private async UniTaskVoid RunDialogueAsync(CancellationToken ct, string line)
        {
            _isDialoguePlaying = true;
            _ = _backUI?.CloseAsync();
            _dialogueUI.SetText(line);
            await _uiModule.PlaySequenceAsync(ct, _dialogueUI);
            _isDialoguePlaying = false;
            if (!ct.IsCancellationRequested)
                RefreshStatusUI();
        }

        private async UniTaskVoid PlayStatusSequenceAsync()
        {
            if (_uiCts == null) return;
            var ct = _uiCts.Token;
            _initializing = true;
            RefreshStatusUI();
            _initializing = false;
            if (!ShouldShowBubble()) return;
            _ = _backUI?.OpenAsync();
            await _uiModule.PlaySequenceAsync(ct, _statusUI);
        }

        private bool ShouldShowBubble()
        {
            if (!_customer.OrderPlaced || _customer.IsAwaitingOrder) return true;
            if (_customer.FoodServed && !_customer.PaymentDone) return true;
            var ticket = _customer.ActiveTicket;
            return ticket != null && ticket.IsPlayerActionable;
        }

        private void RefreshUI() => RefreshStatusUI();

        private void RefreshStatusUI()
        {
            if (_statusUI == null) return;

            if (_contractObj != null)
                _contractObj.CanInteracting = _customer.AssignedSeat != null
                                           || (_customer.FoodServed && !_customer.PaymentDone);

            bool shouldShow = ShouldShowBubble();

            if (!shouldShow)
            {
                if (_statusUI.IsOpen)
                {
                    _backUI?.ClearTint();
                    _ = _backUI?.CloseAsync();
                    _ = _statusUI?.CloseAsync();
                }
                return;
            }

            // 초기화 중이거나 대사 중일 때는 직접 열지 않음
            if (!_statusUI.IsOpen && !_initializing && !_isDialoguePlaying)
            {
                _ = _backUI?.OpenAsync();
                _ = _statusUI?.OpenAsync();
            }

            if (!_customer.OrderPlaced || _customer.IsAwaitingOrder)
            {
                _backUI?.ClearTint();
                _statusUI.ToggleText("...");
                return;
            }

            if (_customer.FoodServed && !_customer.PaymentDone)
            {
                _backUI?.ClearTint();
                _statusUI.ToggleText("계산 대기");
                SetBgAlpha(1f);
                return;
            }

            var ticket        = _customer.ActiveTicket;
            bool isActionable = ticket != null && ticket.IsPlayerActionable;
            var newIcon       = _customer.SelectedFood?.cocktailIcon;

            if (newIcon != null)
            {
                _statusUI.ToggleIcon();
                if (_statusUI.Icon is IStylableUI icon)
                    icon.SetSprite(newIcon).SetRecolorFade(0f);

                if (isActionable)
                    _backUI?.ApplyServeTint();
                else
                {
                    _backUI?.ClearTint();
                    SetBgAlpha(1f);
                }
            }
            else
            {
                _backUI?.ClearTint();
                _statusUI.ToggleText(_customer.SelectedFood?.cocktailName ?? "...");
            }
        }

        private void SetBgAlpha(float alpha)
        {
            if (_backUI?.BackgroundImage == null) return;
            Color c = _backUI.BackgroundImage.color;
            c.a = alpha;
            _backUI.BackgroundImage.color = c;
        }
    }
}
