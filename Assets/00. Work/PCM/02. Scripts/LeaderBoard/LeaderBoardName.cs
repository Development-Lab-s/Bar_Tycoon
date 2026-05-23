using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class LeaderBoardName : MonoBehaviour
{
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
        }
        catch (Exception e)
        {
            Debug.LogError($"초기화 실패 : {e}");
        }
    }
    public void NameSet(string pyName)
    {
        if (!_isInitialized)
            return;

        _ = NameSetAsync(pyName);
    }
    private async Task NameSetAsync(string pyName)
    {
        try
        {
            await AuthenticationService.Instance
                .UpdatePlayerNameAsync(pyName);

            Debug.Log($"닉네임 변경 완료 : {pyName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"닉네임 변경 실패 : {e}");
        }
    }
}
