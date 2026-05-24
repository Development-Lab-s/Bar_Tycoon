using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Customer;
using BBJ.Movement;
using UnityEngine;

namespace BBJ.States
{
    public class CustomerMoveState : CustomerAgentState
    {
        private readonly IPathMovement _movement;

        private bool _isMoveCompleted;

        public CustomerMoveState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _movement = owner.GetModule<IPathMovement>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _isMoveCompleted, CustomerState.Idle);
        }

        public override void Enter()
        {
            base.Enter();
            _isMoveCompleted = false;

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
