using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards
{
    public abstract class AbstractUnlockSO : ScriptableObject
    {
            [SerializeField] private EventChannelSO eventChannelSo;
            public abstract void LevelUpReward();
    }
}