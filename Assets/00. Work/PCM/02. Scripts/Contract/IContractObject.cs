using System;

namespace _00._Work.PCM._02._Scripts
{
    public interface IContractObject
    {
        public event Action OnClick;
        void ExcuteClick();
    }
}