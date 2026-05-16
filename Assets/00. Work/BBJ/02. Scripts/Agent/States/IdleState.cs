using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Movement;
using System.Diagnostics;
using UnityEditor.Searcher;

namespace BBJ.States
{
    public class IdleState : TransitionAgentState
    {
        private readonly IPathMovement _movement;
        private bool _isMoveStarted;
        public IdleState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _movement = owner.GetModule<IPathMovement>();
            AddTransitionToEnum(() => _isMoveStarted, StaffState.Move);

            Debug.Assert(_movement != null, "Agent¿¡ Movement°¡ ´©¶ôµÊ");
        }

        public override void Enter()
        {
            base.Enter();
            _isMoveStarted = false;

            _movement.OnMoveStarted += HandleMoveStarted;
        }

        public override void Exit()
        {
            base.Exit();
            _movement.OnMoveStarted -= HandleMoveStarted;
        }

        private void HandleMoveStarted() => _isMoveStarted = true;
    }
}
