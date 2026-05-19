using _00._Work._Resources._02._Scripts.Agents.Players;
using _00._Work._Resources._02._Scripts.Modules;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace _00._Work.PCM._02._Scripts
{
    public abstract class AbstructContractObject : MonoBehaviour, IContractObject , IModule
    {
        private ModuleOwner _owner;
        [field:SerializeField]public UnityEvent OnClickEvent { get; set; }
        [field: SerializeField]public UnityEvent<int> OnLike { get; set; }

        [field:SerializeField]public CharacterEnum characterEnum { get; }

        public virtual void ExcuteClick()
        {
            OnClickEvent?.Invoke();
        }
        public void Initialize(ModuleOwner owner)
        {
           _owner = owner;
        }
    }
}
