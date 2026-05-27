using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Modules;
using BBJ.Movement;
using BBJ.Schedule;
using UnityEngine;

namespace BBJ.States
{
    public class StaffMoveState : TransitionAgentState
    {
        private readonly IPathMovement    _movement;
        private readonly SchedulingModule _scheduling;
        private readonly IAgentInput      _input;

        private bool _isMoveCompleted;

        public StaffMoveState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _movement   = owner.GetModule<IPathMovement>();
            _scheduling = owner.GetModule<SchedulingModule>();
            _input      = owner.GetModule<IAgentInput>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _isMoveCompleted,      StaffState.Idle);
            AddTransitionToEnum(() => _input.IsInteracting,  StaffState.Interact);
        }

        public override void Enter()
        {
            base.Enter();
            _isMoveCompleted = false;

            if (_scheduling != null && _scheduling.IsWorkPaused)
                _scheduling.Resume();

            _movement.OnMoveCompleted       += HandleMoveCompleted;
            _movement.OnMoveVelocityChanged += HandleVelocityChanged;
        }

        public override void Exit()
        {
            _movement.OnMoveCompleted       -= HandleMoveCompleted;
            _movement.OnMoveVelocityChanged -= HandleVelocityChanged;
        }

        private void HandleVelocityChanged(Vector3 velocity) => _renderer.FlipController(velocity.x);
        private void HandleMoveCompleted() => _isMoveCompleted = true;
    }
}
