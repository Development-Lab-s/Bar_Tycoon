using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
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
        private readonly SchedulingModule _scheduling;
        private readonly IAgentUIModule _uiModule;
        private readonly IAgentInput _input; // 입력을 받기 위해 추가
        private bool _isTriggerCall;
        private CancellationTokenSource _autoCloseCts; // 자동 닫힘 타이머 제어용

        public InteractState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _scheduling = owner.GetModule<SchedulingModule>();
            _uiModule = owner.GetModule<IAgentUIModule>();
            _input = owner.GetModule<IAgentInput>();
            _autoCloseCts = new CancellationTokenSource();
            UtilDebugger.AssertAllAssigned(this);

            // _isTriggerCall이 true가 되면 Idle 상태로 전환
            AddTransitionToEnum(() => _isTriggerCall, StaffState.Idle);
        }

        public override void Enter()
        {
            base.Enter();
            _isTriggerCall = false;

            var contractObj = _owner.GetModule<AbstructContractObject>();
            if (contractObj != null)
            {
                contractObj.IsInteracting = true;
                contractObj.UnHover();
            }
            _uiModule.Get<ContractChat>()?.OpenAsync().Forget();

            _input.OnInteracted += HandleManualClose;
            _autoCloseCts = new CancellationTokenSource();
            AutoCloseRoutine(_autoCloseCts.Token).Forget();

            var chatUI = _uiModule.Get<ContractChat>();
            if (chatUI != null)
            {
                chatUI.Message("출력할 대사 데이터");
                chatUI.OpenAsync().Forget();
            }
        }

        // [이벤트 1] 자동 닫힘 처리
        private async UniTaskVoid AutoCloseRoutine(CancellationToken ct)
        {
            // 매개변수가 아니라 뒤에 .SuppressCancellationThrow() 확장 메서드를 붙여줍니다.
            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: ct)
                .SuppressCancellationThrow();

            if (!isCanceled)
            {
                _isTriggerCall = true; // 타이머가 다 돌았으므로 상태 전환!
            }
        }
        // [이벤트 2] 수동 닫힘 처리
        private void HandleManualClose()
        {
            _isTriggerCall = true; // 플레이어가 클릭했으므로 상태 전환!
        }

        public override void Exit()
        {
            base.Exit();

            // ★ 핵심: 어느 쪽으로든 상태가 종료되면 두 이벤트를 모두 삭제/취소합니다.
            _input.OnInteracted -= HandleManualClose; // 이벤트 구독 해제
            if (_autoCloseCts != null)
            {
                _autoCloseCts.Cancel(); // 진행 중이던 타이머 강제 종료
                _autoCloseCts.Dispose();
                _autoCloseCts = null;
            }

            // Hover 잠금 해제
            var contractObj = _owner.GetModule<AbstructContractObject>();
            if (contractObj != null)
            {
                contractObj.IsInteracting = false;
            }

            // UI 닫기 및 스케줄 재개
            _uiModule.Get<ContractChat>()?.CloseAsync().Forget();
            _scheduling?.Resume();
        }
    }
}