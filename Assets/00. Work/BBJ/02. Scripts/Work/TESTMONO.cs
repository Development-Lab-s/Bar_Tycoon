using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using BBJ.EventSystem;
using BBJ.Scene;
using BBJ.Staff;
using UnityEngine;
using UnityEngine.InputSystem;

public class TESTMONO : MonoBehaviour
{
    [SerializeField] private StoryEpisodeSO targetEpisode;
    public Gamelib.EventSystem.EventChannelSO a;
    public StaffConfigSO s;

    public 
    void Update()
    {
        //if (Keyboard.current.sKey.wasPressedThisFrame)
        //    StoryTransitionContext.Instance.RequestStory(new _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events.StoryEpisodeUnlockRequested
        //        (targetEpisode));
        //if (Keyboard.current.aKey.wasPressedThisFrame)
        //    a.RaiseEvent(new StaffSpawnEvent().Init(s,(pos)=>transform.position = pos));
    }
}
