using _00._Work._Resources._02._Scripts.Agents.Players;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Modules; // IHoverRenderer 네임스페이스
using UnityEngine;
using UnityEngine.Events;

namespace _00._Work.PCM._02._Scripts
{
    public abstract class AbstructContractObject : MonoBehaviour, IContractObject, IModule
    {
        private ModuleOwner _owner;
        private IHoverRenderer _hoverRenderer;
        private bool isHover;
        public bool IsInteracting { get; set; }
        [field: SerializeField] public UnityEvent OnClickEvent { get; set; }
        [field: SerializeField] public CharacterEnum characterEnum { get; set; }
        [field: SerializeField] public UnityEvent<int> OnLike { get; set; }

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _hoverRenderer = _owner.GetModule<IHoverRenderer>();
        }

        public virtual void ExcuteClick()
        {
            OnClickEvent?.Invoke();
        }

        public virtual void Hover()
        {
            if (IsInteracting) return;

            isHover = true;
            _hoverRenderer?.EnableHoverEffect();
        }

        public virtual void UnHover()
        {
            isHover = false;
            _hoverRenderer?.DisableHoverEffect();
        }
    }
}