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
        private AgentParticleModule          _particles;
        private bool _isTriggerCall;
        private CancellationTokenSource _autoCloseCts;

        public InteractState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _scheduling = owner.GetModule<SchedulingModule>();
            _uiModule = owner.GetModule<IAgentUIModule>();
            _input = owner.GetModule<IAgentInput>();
            _autoCloseCts = new CancellationTokenSource();
            UtilDebugger.AssertAllAssigned(this);
            _particles = owner.GetModule<AgentParticleModule>();

            // _isTriggerCall�� true�� �Ǹ� Idle ���·� ��ȯ
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
            _input.OnInteracted += HandleManualClose;
            _autoCloseCts = new CancellationTokenSource();
            AutoCloseRoutine(_autoCloseCts.Token).Forget();

            var chatUI = _uiModule.Get<ContractChat>();
            if (chatUI != null)
            {
                var chatProvider = _owner.GetModule<IChatProvider>();
                var message = chatProvider?.ChatMessage;
                if (!string.IsNullOrEmpty(message))
                    chatUI.Message(message);
                chatUI.OpenAsync().Forget();
            }
        }

        // [�̺�Ʈ 1] �ڵ� ���� ó��
        private async UniTaskVoid AutoCloseRoutine(CancellationToken ct)
        {
            // �Ű������� �ƴ϶� �ڿ� .SuppressCancellationThrow() Ȯ�� �޼��带 �ٿ��ݴϴ�.
            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: ct)
                .SuppressCancellationThrow();

            if (!isCanceled)
            {
                _isTriggerCall = true; // Ÿ�̸Ӱ� �� �������Ƿ� ���� ��ȯ!
            }
        }
        // [�̺�Ʈ 2] ���� ���� ó��
        private void HandleManualClose()
        {
            _isTriggerCall = true; // �÷��̾ Ŭ�������Ƿ� ���� ��ȯ!
        }

        public override void Exit()
        {
            base.Exit();

            // �� �ٽ�: ��� �����ε� ���°� ����Ǹ� �� �̺�Ʈ�� ��� ����/����մϴ�.
            _input.OnInteracted -= HandleManualClose; // �̺�Ʈ ���� ����
            if (_autoCloseCts != null)
            {
                _autoCloseCts.Cancel(); // ���� ���̴� Ÿ�̸� ���� ����
                _autoCloseCts.Dispose();
                _autoCloseCts = null;
            }

            // Hover ��� ����
            var contractObj = _owner.GetModule<IHoverable>();
            if (contractObj != null)
            {
                contractObj.CanInteracting = true;
            }

            // UI �ݱ� �� ������ �簳
            _uiModule.Get<ContractChat>()?.CloseAsync().Forget();
            _scheduling?.Resume();
        }
    }
}