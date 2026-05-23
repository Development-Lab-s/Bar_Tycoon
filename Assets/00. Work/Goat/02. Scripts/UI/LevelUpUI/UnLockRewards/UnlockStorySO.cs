using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards
{
    [CreateAssetMenu(fileName = "unlockStory", menuName = "SO/unlock/UnlockStory", order = 0)]
    public class UnlockStorySO : AbstractUnlockSO
    {
        public override void LevelUpReward()
        {
            
        }

        public override List<Sprite> GetSprite()
        {
            List<Sprite> sprites = new List<Sprite>();
            return  sprites;
        }
    }
}