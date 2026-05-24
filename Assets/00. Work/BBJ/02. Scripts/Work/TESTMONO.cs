using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using BBJ.Scene;
using UnityEngine;
using UnityEngine.InputSystem;

public class TESTMONO : MonoBehaviour
{
    [SerializeField] private StoryEpisodeSO targetEpisode;
    public Gamelib.EventSystem.EventChannelSO a;

    public 
    void Update()
    {
        if (Keyboard.current.sKey.wasPressedThisFrame)
            StoryTransitionContext.Instance.RequestStory(new _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events.StoryEpisodeUnlockRequested
                (targetEpisode));
    }
}
