using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using UnityEngine.SocialPlatforms.Impl;
using System;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

public class LeaderBoardManager : MonoBehaviour
{
    [SerializeField]private LeaderBoardInfoSO _leaderboardInfoSO;
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
            string playerName = AuthenticationService.Instance.PlayerName;
            var scoreResponse = await LeaderboardsService.Instance.GetPlayerScoreAsync("Bar_Tycoon");
            _leaderboardInfoSO.UpdateData(playerName , scoreResponse.Score, scoreResponse.Rank); 
        }
        catch (Exception e)
        {
            Debug.LogError($"점수 가져오기 실패: {e.Message}");
            return;
        }
    }
}