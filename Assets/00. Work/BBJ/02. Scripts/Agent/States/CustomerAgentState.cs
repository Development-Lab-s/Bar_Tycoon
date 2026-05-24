using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Customer;

namespace BBJ.States
{
    public abstract class CustomerAgentState : TransitionAgentState
    {
        protected readonly CustomerAgent _customer;

        protected CustomerAgentState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _customer = owner as CustomerAgent;
        }

        public override void Enter()
        {
            int layer = _customer?.AssignedSeat != null ? 1 : 0;
            _renderer.PlayClip(_clipHash, layer);
            _isTriggerCall = false;
        }
    }
}
