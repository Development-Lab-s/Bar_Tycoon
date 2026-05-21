using Gamelib.EventSystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.InputSystem;

public class LeaderBoardManager : MonoBehaviour
{
    [System.Serializable]
    public class LeaderboardExtraData
    {
        public int playerLevel;
        public string favoriteCharacter;
    }

    [SerializeField]private EventChannelSO _eventChannel;
    private int limit = 50;
    public List<GameObject> RankList = new List<GameObject>();
    string favoriteCharacter;
    int level = 10;
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
    private async Task ScoreAddAsync(int gold)
    {
        LeaderboardExtraData extraData = new LeaderboardExtraData
        {
            playerLevel = level,
            favoriteCharacter = favoriteCharacter
        };
        string jsonMetadata = JsonConvert.SerializeObject(extraData);

        var options = new AddPlayerScoreOptions
        {
            Metadata = new Dictionary<string, string> { { "extra_info", jsonMetadata } }
        };

        try
        {
            var scoreEntry = await LeaderboardsService.Instance.AddPlayerScoreAsync("Bar_Tycoon",gold, options);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[리더보드] 제출 실패: {e.Message}");
        }
    }
    private async Task NameSetAsync(string pyName)
    {
        Debug.Log(pyName);
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
            foreach (var entry in scoresResponse.Results)
            {
                string playerName = entry.PlayerName;
                double gold = entry.Score;
                int rank = entry.Rank + 1;

                int level = 1;
                string favoriteChar = "None";

                if (entry.Metadata != null)
                {
                    try
                    {
                        string jsonMetadata = entry.Metadata;

                        if (jsonMetadata.Contains("playerLevel"))
                        {
                            LeaderboardExtraData extraData = JsonConvert.DeserializeObject<LeaderboardExtraData>(jsonMetadata);

                            level = extraData.playerLevel;
                            favoriteChar = extraData.favoriteCharacter;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"메타데이터 파싱 실패 (기존 데이터 포맷 다름): {ex.Message}");
                    }
                }
                Debug.Log($"[{rank}등] {playerName} | 골드: {gold} | 레벨: {level} | 최애캐: {favoriteChar}");

                if (entry.PlayerId == myid)
                {
                    Debug.Log("이 데이터는 현재 로그인한 내 리더보드 정보입니다!");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"점수 가져오기 실패: {e.Message}");
            return;
        }
    }
}