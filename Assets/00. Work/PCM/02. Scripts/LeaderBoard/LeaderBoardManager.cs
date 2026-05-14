using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using UnityEngine.SocialPlatforms.Impl;
using System;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using Gamelib.EventSystem;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class LeaderBoardManager : MonoBehaviour
{
    [SerializeField]private EventChannelSO _eventChannel;
    private int limit = 50;
    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }
    private async void Update()
    {
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            await GetMyLeaderboardInfo();
        }
    }
    public void ScoreAdd(int level)
    {
        _ = ScoreAddAsync(level);
    }
    public void NameSet(string pyName)
    {
        _ = NameSetAsync(pyName);
    }
    private async Task ScoreAddAsync(int level)
    {
        level = Random.Range(0, level);
        Debug.Log($"님 랜덤 점수{level}");
        await LeaderboardsService.Instance.AddPlayerScoreAsync("Bar_Tycoon",level);
    }
    private async Task NameSetAsync(string pyName)
    {
        await AuthenticationService.Instance.UpdatePlayerNameAsync(pyName);
    }
    public async Task GetMyLeaderboardInfo()
    {
        try
        {
            string myid = AuthenticationService.Instance.PlayerId;
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync("Bar_Tycoon", new GetScoresOptions
            {
                Offset = 0,
                Limit = limit
            });

            _eventChannel.RaiseEvent(new LeaderBoardEvent(myid, scoresResponse.Results));
            //가져온 리스트를 루프 돌며 처리하거나, 리스트 전용 이벤트를 발생시킵니다.
        }
        catch (Exception e)
        {
            Debug.LogError($"점수 가져오기 실패: {e.Message}");
            return;
        }
    }
}