using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Modules;
using BBJ.Movement;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace BBJ.States
{
    public class InteractState : TransitionAgentState
    {
        private readonly IAgentInput   _input;
        private readonly IPathMovement _movement;

        private bool _isDone;

        private const float Duration = 2f;

        public InteractState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _input    = owner.GetModule<IAgentInput>();
            _movement = owner.GetModule<IPathMovement>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _isDone, StaffState.Idle);
        }

        public override void Enter()
        {
            base.Enter();
            _isDone = false;

            _movement.PauseMovement();

            RunTimerAsync().Forget();
        }

        private async UniTaskVoid RunTimerAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Duration))
                .SuppressCancellationThrow();
        }

        public override void Exit()
        {
            base.Exit();

            _input.IsInteracting = false;
            _movement.ResumeMovement();
        }
    }
}
