using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BBJ.States
{
    public abstract class TransitionAgentState : AgentState, ITransitionState
    {
        protected readonly List<StateTransition> transitions;
        public IReadOnlyList<StateTransition> Transitions => transitions;
        protected TransitionAgentState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            transitions = new List<StateTransition>();
        }
        public void AddTransitionToEnum<T>(Func<bool> condition, T toState) where T : struct, Enum
            => AddTransitionToIndex(condition, Unsafe.As<T, int>(ref toState));
        public void AddTransitionToIndex(Func<bool> condition, int toState)
            => transitions.Add(new StateTransition(condition, toState));
    }
}
