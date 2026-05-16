using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;

namespace BBJ.States
{
    public class WorkState : AgentState
    {
        public WorkState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam) { }
    }
}
