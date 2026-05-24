using _00._Work._Resources._02._Scripts.Agents.Players;
using _00._Work._Resources._02._Scripts.Modules;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace _00._Work.PCM._02._Scripts
{
    public abstract class AbstructContractObject :MonoBehaviour,IContractObject,IModule
    {
        private ModuleOwner _owner;
        [field: SerializeField]public UnityEvent OnClickEvent { get; set; }
        [field: SerializeField]public CharacterEnum characterEnum { get; set; }
        [field: SerializeField]public UnityEvent<int> OnLike { get; set; }

        [SerializeField]private Renderer spriteRenderer;
        Material material;
        protected bool isHover;
        private float tintValue = 0.21f;
        private void Start()
        {
            material = spriteRenderer.material;
            material.SetFloat("_OuterOutlineFade", 0);
            material.SetFloat("_StrongTintFade", 0);
        }
        public virtual void ExcuteClick()
        {
            OnClickEvent?.Invoke();
        }
        public virtual void Hover()
        {
            isHover = true;
            material = spriteRenderer.material;
            material.SetFloat("_OuterOutlineFade", 1);
            material.SetFloat("_StrongTintFade", tintValue);
        }

        public virtual void UnHover()
        {
            isHover = false;
            material = spriteRenderer.material;
            material.SetFloat("_OuterOutlineFade", 0);
            material.SetFloat("_StrongTintFade", 0);
        }

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }
    }
}