using System;
using UnityEngine.Events;

namespace _00._Work.PCM._02._Scripts
{
    public interface IContractObject
    {
        public UnityEvent OnClickEvent { get; }
        void ExcuteClick();
        public bool CanInteracting { get; set; }
    }
    public interface IHoverable : IContractObject
    {
        bool TryHover();
        void UnHover();

    }
    public interface ILikeabillty
    {
        public CharacterEnum characterEnum { get; }
        public UnityEvent<int> OnLike { get; }
    }

    public interface IChatProvider
    {
        string ChatMessage { get; }
    }
}