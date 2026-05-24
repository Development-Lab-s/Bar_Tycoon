using UnityEngine;

namespace BBJ.Modules
{
    public class AnimationEventForwarder : MonoBehaviour
    {
        private SpineAgentRenderer _renderer;

        public void Initialize(SpineAgentRenderer renderer)
        {
            _renderer = renderer;
        }

        private void EndTrigger() => _renderer.EndTrigger();
        private void AttackTrigger() => _renderer.AttackTrigger();
        private void OpenCounterTrigger() => _renderer.OpenCounterTrigger();
        private void CloseCounterTrigger() => _renderer.CloseCounterTrigger();
    }
}
