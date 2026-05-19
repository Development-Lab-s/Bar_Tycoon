using _00._Work._Resources._02._Scripts.Agents;
using UnityEngine;

namespace BBJ.Actions
{
    public abstract class AgentActionBase : MonoBehaviour, IAgentAction
    {
        protected Agent _owner;

        public virtual void InitOwner(Agent owner)
        {
            this._owner = owner;
        }
    }
}
