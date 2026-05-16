using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Movement;
using System;
using UnityEngine;

namespace BBJ.States
{
    public class MoveState : TransitionAgentState
    {
        private readonly IPathMovement _movement;
        private bool isMoveCompleted;
        public MoveState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _movement = owner.GetModule<IPathMovement>();
            AddTransitionToEnum(() => isMoveCompleted, StaffState.Idle);

#if UNITY_EDITOR
            Debug.Assert(_movement != null, $"{owner.name}에 Movement가 누락되었습니다.");
#endif
        }

        public override void Enter()
        {
            base.Enter();
            isMoveCompleted = false;

            _movement.OnMoveCompleted += HandleMoveCompleted;
            _movement.OnMoveVelocityChanged += HandleVelocityChanged;
        }
        public override void Exit()
        {
            _movement.OnMoveCompleted -= HandleMoveCompleted;
            _movement.OnMoveVelocityChanged -= HandleVelocityChanged;
        }
        void HandleVelocityChanged(Vector3 velocity) => _renderer.FlipController(velocity.x);
        void HandleMoveCompleted() => isMoveCompleted = true;
    }
   
}
