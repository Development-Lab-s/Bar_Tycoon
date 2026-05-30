using _00._Work._Resources._02._Scripts.Modules;
using BBJ.EventSystem;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SideLpController : MonoBehaviour, IModule
{
    [SerializeField] private EventChannelSO _LPchannel;
    [SerializeField] private EventChannelSO _sceneChannel;
    [SerializeField] private EventChannelSO _soundChannel;
    [SerializeField] private LpStateSO _lpState;

    [SerializeField] private TextMeshProUGUI text;

    [Header("LP 모듈 변수들")]
    [SerializeField] private GameObject lpItemPrefab;
    [SerializeField] private LpBoxImage lpImage;
    [SerializeField] private Image currentLpImage;

    private Transform parentTrm;

    private readonly Dictionary<int, LPBOX> _lpBoxDict = new();

    private int _currentActiveId = -1;
    private bool _bgmNeedsResume = false;
    private bool _isPaused = false;

    private ModuleOwner _owner;

    public void Initialize(ModuleOwner owner)
    {
        _owner = owner;
        _LPchannel.AddListener<LpConncetEvent>(EventPlayLp);
        _sceneChannel?.AddListener<SceneTypeChangedEvent>(OnSceneChanged);
        parentTrm = transform;
        CreateLPBoxes();
        int initialIndex = _lpState != null ? _lpState.SelectedIndex : 0;
        InitLp(initialIndex);
    }

    private void CreateLPBoxes()
    {
        _lpBoxDict.Clear();

        int count =
            Enum.GetValues(typeof(BgmSounds)).Length;

        for (int i = 0; i < count; i++)
        {
            GameObject lpObj =
                Instantiate(lpItemPrefab, parentTrm);
            LPBOX lpBox =
                lpObj.GetComponentInChildren<LPBOX>();
            lpBox.GetComponent<Image>().sprite = lpImage.image[i];
            if (lpBox == null)
            {
                Debug.LogError(
                    $"LPBOX ���� : {lpObj.name}");

                continue;
            }
            lpBox.SetUp(i);

            lpBox.OnLPClicked -= PlayLp;
            lpBox.OnLPClicked += PlayLp;

            _lpBoxDict.Add(i, lpBox);
        }
    }
    public void EventPlayLp(LpConncetEvent evt)
    {
        PlayLp(evt.Id);
    }
    public void PlayLp(int id)
    {
        if (!_lpBoxDict.ContainsKey(id))
            return;
        if (_currentActiveId == id)
            return;
        if (_currentActiveId != -1)
            _lpBoxDict[_currentActiveId].StopLP();
        _isPaused = false;
        _lpBoxDict[id].Select();
        text.text = _lpBoxDict[id].ChangeName();
        if (currentLpImage != null) currentLpImage.sprite = lpImage.image[id];
        _currentActiveId = id;
        if (_lpState != null) _lpState.SelectedIndex = id;
    }

    public void TogglePause()
    {
        if (_currentActiveId < 0) return;
        _isPaused = !_isPaused;
        if (_isPaused)
            _soundChannel?.RaiseEvent(new PauseSoundEvent(SoundChannelId.Bgm));
        else
            _soundChannel?.RaiseEvent(new ResumeSoundEvent(SoundChannelId.Bgm));
    }

    private void InitLp(int id)
    {
        if (!_lpBoxDict.ContainsKey(id)) return;
        _lpBoxDict[id].SelectSilent();
        text.text = _lpBoxDict[id].ChangeName();
        if (currentLpImage != null) currentLpImage.sprite = lpImage.image[id];
        _currentActiveId = id;
        ResumeBgm();
    }

    private void ResumeBgm()
    {
        if (_currentActiveId < 0) return;
        _soundChannel?.RaiseEvent(new PlaySoundEvent((BgmSounds)_currentActiveId, Vector3.zero, SoundChannelId.Bgm));
    }

    private void OnSceneChanged(SceneTypeChangedEvent e)
    {
        switch (e.Current)
        {
            case SceneType.Cocktail:
                // BGM이 계속 재생 중이므로 재개 불필요
                break;
            case SceneType.Main:
                if (_bgmNeedsResume)
                {
                    _bgmNeedsResume = false;
                    ResumeBgm();
                }
                break;
            default:
                _bgmNeedsResume = true;
                break;
        }
    }

    private void OnDestroy()
    {
        foreach (var box in _lpBoxDict.Values)
        {
            if (box != null)
                box.OnLPClicked -= PlayLp;
        }
        _LPchannel.RemoveListener<LpConncetEvent>(EventPlayLp);
        _sceneChannel?.RemoveListener<SceneTypeChangedEvent>(OnSceneChanged);
    }
}