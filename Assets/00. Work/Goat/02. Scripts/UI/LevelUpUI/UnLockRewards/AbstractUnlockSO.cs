using System.Collections.Generic;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards
{
    public abstract class AbstractUnlockSO : ScriptableObject
    {
            [SerializeField] protected EventChannelSO eventChannelSo;
            public abstract void LevelUpReward();
            public abstract List<Sprite> GetSprite();
    }
}