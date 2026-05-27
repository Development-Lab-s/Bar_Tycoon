using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.PCM._02._Scripts;

namespace BBJ.Modules
{
    public class AgentInputModule : AbstructContractObject, IAgentInput
    {
        public bool IsInteracting { get; set; }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            OnLike.RemoveListener(HandleLike);
            OnLike.AddListener(HandleLike);
        }

        public override void ExcuteClick()
        {
            base.ExcuteClick();
            TryInteract();
        }

        private void HandleLike(int _) => TryInteract();

        private void TryInteract()
        {
            if (!IsInteracting)
                IsInteracting = true;
        }
    }
}
