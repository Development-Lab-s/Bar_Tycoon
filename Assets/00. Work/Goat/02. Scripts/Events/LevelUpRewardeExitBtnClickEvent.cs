using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class LevelUpRewardeExitBtnClickEvent : GameEvent
    { 
        public bool playGoScene; 
        
        public LevelUpRewardeExitBtnClickEvent Init(bool playScene = false)
        {
            playGoScene = playScene;
            return this;
        }
    }
}