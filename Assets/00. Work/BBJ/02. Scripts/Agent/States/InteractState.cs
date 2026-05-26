using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.Module;
using _00._Work.PCM._02._Scripts;
using Assets._00._Work.PCM._02._Scripts.Contract;
using BBJ.Modules;
using BBJ.Schedule;
using BBJ.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace BBJ.States
{
    public class InteractState : TransitionAgentState
    {
        private readonly SchedulingModule    _scheduling;
        private readonly IAgentUIModule      _uiModule;
        private readonly IAgentInput         _input;
        private readonly AgentParticleModule _particles;
        private readonly ContractChat        _contractChat;
        private bool _isTriggerCall;
        private CancellationTokenSource _cts;

        public InteractState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _scheduling   = owner.GetModule<SchedulingModule>();
            _uiModule     = owner.GetModule<IAgentUIModule>();
            _input        = owner.GetModule<IAgentInput>();
            _particles    = owner.GetModule<AgentParticleModule>();
            _contractChat = _uiModule?.Get<ContractChat>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _isTriggerCall, StaffState.Idle);
        }

        public override void Enter()
        {
            base.Enter();
            _isTriggerCall = false;

            var contractObj = _owner.GetModule<IHoverable>();
            if (contractObj != null)
            {
                contractObj.CanInteracting = false;
                contractObj.UnHover();
            }

            _particles?.PlayParticle(ParticleType.HEART);

            var chatProvider = _owner.GetModule<IChatProvider>();
            var message = chatProvider?.ChatMessage;
            if (!string.IsNullOrEmpty(message))
                _contractChat?.Message(message);

            _input.OnInteracted += HandleManualClose;
            _cts = new CancellationTokenSource();
            RunInteractAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid RunInteractAsync(CancellationToken ct)
        {
            if (_contractChat != null)
                await _uiModule.PlaySequenceAsync(ct, _contractChat);
            else
                await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: ct)
                    .SuppressCancellationThrow();

            if (!ct.IsCancellationRequested)
                _isTriggerCall = true;
        }

        private void HandleManualClose()
        {
            _cts?.Cancel();
            _isTriggerCall = true;
        }

        public override void Exit()
        {
            base.Exit();
            _input.OnInteracted -= HandleManualClose;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            var contractObj = _owner.GetModule<IHoverable>();
            if (contractObj != null)
                contractObj.CanInteracting = true;

            _scheduling?.Resume();
        }
    }
}
