using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.EventSystem;
using System.Net.NetworkInformation;
using UnityEngine;

public class LeaderBoardUi : MonoBehaviour , IModule
{
    [field:SerializeField]public EventChannelSO _eventChannel { get; set; }
    private ModuleOwner _owner;
    public void Initialize(ModuleOwner owner)
    {
        _owner = owner;
        _eventChannel.AddListener<LeaderBoardEvent>(UpdateLeaderBoardUI);
    }
    public void UpdateLeaderBoardUI(LeaderBoardEvent evt)
    {
        for (int i =0; i < evt.Entris.Count; i++)
        {
            if(evt.Entris[i].PlayerId == evt.Id)
            {
                Debug.Log($"당신의 기록은, Score: {evt.Entris[i].Score}");
            }
            else
                Debug.Log($"Id:{evt.Entris[i].PlayerId}, Name:{evt.Entris[i].PlayerName}, Score: {evt.Entris[i].Score}");
            
        }
    }
}
