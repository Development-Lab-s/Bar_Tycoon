using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using BBJ.States;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BBJ
{
    public abstract class AbstractTycoonAgent : Agent
    {
        [SerializeField] private StateListSO stateList;
        private AgentStateMachine _stateMachine;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            _stateMachine = new AgentStateMachine(this, stateList.states);
        }

        protected virtual void Update()
        {
            var state = _stateMachine.CurrentState as ITransitionState;
            if(state == null) return;

            for (int i = 0; i < state.Transitions.Count; ++i)
            {
                if (state.Transitions[i].CheckAndTransition())
                    ChangeState(state.Transitions[i].ToStateindex);
            }

            _stateMachine.UpdateMachine();
        }
        public void ChangeState(int nextState) => _stateMachine.ChangeState(nextState);
        public virtual void ChangeState<T>(T nextState) where T : struct, Enum
        {
            int nextIndex = Unsafe.As<T, int>(ref nextState);
            _stateMachine.ChangeState(nextIndex);
        }
    }
}