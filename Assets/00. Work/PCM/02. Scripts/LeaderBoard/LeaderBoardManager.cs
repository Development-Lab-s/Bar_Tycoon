using _00._Work.Goat._02._Scripts.Coin.CoinDatas;
using _00._Work.Goat._02._Scripts.Exp.ExpDatas;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public struct LeaderBoardData
{
    public string Name;
    public double Gold;
    public int Level;
    public string FavoriteCharacter;
}

public class LeaderBoardManager : MonoBehaviour
{
    [Serializable]
    public class LeaderboardExtraData
    {
        public int playerLevel;
        public string favoriteCharacter;
    }

    [SerializeField] private LikeitemListSo listSo;
    [SerializeField] private ExpData expData;
    [SerializeField] private CoinData coinData;
    [SerializeField]private TextMeshProUGUI nicknameSetting;
    private int limit = 50;

    public List<LeaderBoardData> infoTuple = new();
    private bool _isInitialized;

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }
            _isInitialized = true;
            nicknameSetting.text = GetMyNickName();
        }
        catch (Exception e)
        {
            Debug.LogError($"초기화 실패 : {e}");
        }
    }

    private void Update()
    {
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            _ = GetMyLeaderboardInfo();
        }
    }

    public void ScoreAdd()
    {
        _ = ScoreAddAsync();
    }

    public void NameSet(string pyName)
    {
        if (!_isInitialized)
            return;

        _ = NameSetAsync(pyName);
    }

    private async Task ScoreAddAsync()
    {
        LeaderboardExtraData extraData =
            new LeaderboardExtraData
            {
                playerLevel = expData.currentLevel,
                favoriteCharacter = listSo.MostCharacter()
            };
        Debug.Log(listSo.MostCharacter());
        string jsonMetadata =
            JsonConvert.SerializeObject(extraData);

        var options = new AddPlayerScoreOptions
        {
            Metadata = new Dictionary<string, string>
            {
                { "extra_info", jsonMetadata }
            }
        };

        try
        {
            await LeaderboardsService.Instance
                .AddPlayerScoreAsync(
                    "Bar_Tycoon",
                    coinData.coin,
                    options);

        }
        catch (Exception e)
        {
            Debug.LogError($"[리더보드] 제출 실패 : {e}");
        }
    }

    private async Task NameSetAsync(string pyName)
    {
        try
        {
            await AuthenticationService.Instance
                .UpdatePlayerNameAsync(pyName);
            nicknameSetting.text = GetMyNickName();
            Debug.Log($"닉네임 변경 완료 : {pyName}");
            //nicknameSetting.text = GetMyNickName();
        }
        catch (Exception e)
        {
            Debug.LogError($"닉네임 변경 실패 : {e}");
        }
    }
    public async void GetMyLeaderboardInfoNasync()
    {
        await GetMyLeaderboardInfo();
    }
    public async Task GetMyLeaderboardInfo()
    {
        await ScoreAddAsync();
        try
        {
            string myId =
                AuthenticationService.Instance.PlayerId;

            infoTuple.Clear();

            var scoresResponse =
                await LeaderboardsService.Instance
                    .GetScoresAsync(
                        "Bar_Tycoon",
                        new GetScoresOptions
                        {
                            Offset = 0,
                            Limit = limit,
                            IncludeMetadata = true
                        });

            foreach (var entry in scoresResponse.Results)
            {
                string playerName = entry.PlayerName;

                if (Regex.IsMatch(playerName, @"#\d{4}$"))
                {
                    playerName =
                        playerName.Substring(0, playerName.Length - 5);
                }

                double gold = entry.Score;

                int level = 1;
                string favoriteChar = "None";
                if (!string.IsNullOrEmpty(entry.Metadata))
                {
                    try
                    {
                        Dictionary<string, string> metadata =
                            JsonConvert.DeserializeObject
                            <Dictionary<string, string>>(
                                entry.Metadata);

                        if (metadata != null &&
                            metadata.TryGetValue(
                                "extra_info",
                                out string extraJson))
                        {
                            LeaderboardExtraData extraData =
                                JsonConvert.DeserializeObject
                                <LeaderboardExtraData>(
                                    extraJson);

                            if (extraData != null)
                            {
                                level = extraData.playerLevel;
                                favoriteChar =
                                    extraData.favoriteCharacter;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"[{playerName}] 메타데이터 파싱 실패 : {ex}");
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"[{playerName}] Metadata 없음");
                }

                LeaderBoardData data =
                    new LeaderBoardData
                    {
                        Name = playerName,
                        Gold = gold,
                        Level = level,
                        FavoriteCharacter = favoriteChar
                    };

                infoTuple.Add(data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"점수 가져오기 실패 : {e}");
        }
    }
    public string GetMyNickName()
    {
        Debug.Log(_isInitialized);
        if (!_isInitialized)
            return string.Empty;

        string playerName =
            AuthenticationService.Instance.PlayerName;

        if (Regex.IsMatch(playerName, @"#\d{4}$"))
        {
            playerName =
                playerName.Substring(0, playerName.Length - 5);
        }

        return playerName;
    }
}