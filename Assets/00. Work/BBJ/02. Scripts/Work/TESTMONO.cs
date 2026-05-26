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
        //if (keyboard.current.skey.waspressedthisframe)
        //    storytransitioncontext.instance.requeststory(new _00._work.cheolyee._02._scripts.story.runtime.shared.events.storyepisodeunlockrequested
        //        (targetepisode));
        if (Keyboard.current.aKey.wasPressedThisFrame)
            a.RaiseEvent(new StaffSpawnEvent().Init(s, (pos) => transform.position = pos));
    }
}
