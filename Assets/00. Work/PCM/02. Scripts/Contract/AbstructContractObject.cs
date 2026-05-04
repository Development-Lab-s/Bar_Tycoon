using _00._Work._Resources._02._Scripts.Agents.Players;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace _00._Work.PCM._02._Scripts
{
    public abstract class AbstructContractObject : MonoBehaviour, IContractObject
    {
        public UnityEvent OnClickEvent;

        public virtual void ExcuteClick()
        {
            Debug.Log("Áý");
            OnClickEvent?.Invoke();
        }
    }
}
