using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using Gamelib.EventSystem;
using UnityEngine;

public class UnlockStory : MonoBehaviour
{
    [SerializeField]private EventChannelSO storyCommandChannel;
    [SerializeField]private StoryEpisodeSO[] episode;
    public void UnLock(int index)
    {
        storyCommandChannel.RaiseEvent(new StoryEpisodeLaunchRequested(episode[index]));
    }
}
