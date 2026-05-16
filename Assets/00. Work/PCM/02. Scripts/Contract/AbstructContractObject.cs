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
        public UnityEvent OnClickEvent;
        private ModuleOwner _owner;
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
