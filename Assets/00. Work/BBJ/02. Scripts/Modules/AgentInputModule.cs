using System;
using _00._Work.PCM._02._Scripts;

namespace BBJ.Modules
{
    public class AgentInputModule : AbstructContractObject, IAgentInput
    {
        public event Action OnInteracted;

        public override void ExcuteClick()
        {
            base.ExcuteClick();
            OnInteracted?.Invoke();
        }
    }
}
