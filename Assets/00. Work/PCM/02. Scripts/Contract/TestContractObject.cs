using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.PCM._02._Scripts;
using BBJ.Modules;
using UnityEngine;
using UnityEngine.Events;

namespace Assets._00._Work.PCM._02._Scripts
{
    public class TestContractObject : MonoBehaviour, IContractObject, IModule, IHoverable, ILikeabillty
    {
        private ModuleOwner _owner;
        private IHoverRenderer _hoverRenderer;
        private bool isHover;
        public bool CanInteracting { get; set; }
        [field: SerializeField] public UnityEvent OnClickEvent { get; set; }
        [field: SerializeField] public CharacterEnum characterEnum { get; set; }
        [field: SerializeField] public UnityEvent<int> OnLike { get; set; }

        public virtual void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            CanInteracting = true;
            _hoverRenderer = _owner.GetModule<IHoverRenderer>();
        }

        public virtual void ExcuteClick()
        {
            OnClickEvent?.Invoke();
        }

        public virtual bool TryHover()
        {
            if (isActiveAndEnabled && isHover == false)
            {
                _hoverRenderer?.EnableHoverEffect();
                isHover = true;
                return true;
            }
            return false;
        }

        public virtual void UnHover()
        {
            isHover = false;
            _hoverRenderer?.DisableHoverEffect();
        }
    }
}