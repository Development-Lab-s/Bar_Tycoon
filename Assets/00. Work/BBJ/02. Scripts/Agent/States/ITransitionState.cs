using System;
using System.Collections.Generic;

namespace BBJ.States
{

    public interface ITransitionState
    {
        public IReadOnlyList<StateTransition> Transitions { get; }
        public void AddTransitionToEnum<T>(Func<bool> condition, T toState) where T : struct, Enum;
        public void AddTransitionToIndex(Func<bool> condition, int toState);
    }

    public struct StateTransition
    {
        public event Func<bool> Condition;
        public int ToStateindex;
        public StateTransition(Func<bool> condition, int toState)
        {
            Condition = condition;
            this.ToStateindex = toState;
        }
        public bool CheckAndTransition() => Condition.Invoke();
    }
}
