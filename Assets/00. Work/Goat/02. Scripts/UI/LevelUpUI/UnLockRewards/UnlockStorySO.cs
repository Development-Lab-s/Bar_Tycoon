using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Save;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.UnLockRewards
{
    [CreateAssetMenu(fileName = "unlockStory", menuName = "SO/unlock/UnlockStory", order = 0)]
    public class UnlockStorySO : AbstractUnlockSO
    {
        [SerializeField] private StoryEpisodeSO targetEpisode;
        [SerializeField] private Sprite sprite;
        [SerializeField] private string description;
        public override void LevelUpReward()
        {
            eventChannelSo?.RaiseEvent(new StoryEpisodeUnlockRequested(targetEpisode));
        }

        public override List<Sprite> GetSprite()
        {
            List<Sprite> sprites = new List<Sprite>();
            sprites.Add(sprite);
            return  sprites;
        }

        public override List<string> GetDescription()
        {
            List<string> strings = new List<string>();
            strings.Add(description);
            return strings;
        }
    }
}