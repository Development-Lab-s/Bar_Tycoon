using System;
using UnityEngine.Events;

namespace _00._Work.PCM._02._Scripts
{
    public interface IContractObject
    {
        public CharacterEnum characterEnum { get; }
        public UnityEvent OnClickEvent { get; }
        public UnityEvent<int> OnLike { get; }
        void ExcuteClick();
    }
}