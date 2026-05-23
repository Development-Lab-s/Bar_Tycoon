using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SideLpController : MonoBehaviour, IModule
{
    [SerializeField] private EventChannelSO _LPchannel;

    [SerializeField] private TextMeshProUGUI text;

    [Header("LP 부모 프리팹")]
    [SerializeField] private GameObject lpItemPrefab;

    private Transform parentTrm;

    private readonly Dictionary<int, LPBOX> _lpBoxDict = new();

    private int _currentActiveId = -1;
    private int _myid = 0;

    private ModuleOwner _owner;

    public void Initialize(ModuleOwner owner)
    {
        _owner = owner;
        _LPchannel.AddListener<LpConncetEvent>(EventPlayLp);
        parentTrm = transform;
        CreateLPBoxes();
        PlayLp(_myid);
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
            if (lpBox == null)
            {
                Debug.LogError(
                    $"LPBOX 없음 : {lpObj.name}");

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
        _myid = id;
        if (!_lpBoxDict.ContainsKey(id))
            return;
        if (_currentActiveId == id)
            return;
        if (_currentActiveId != -1)
        {
            _lpBoxDict[_currentActiveId].StopLP();
        }
        _lpBoxDict[id].Select();
        text.text =
            _lpBoxDict[id].ChangeName();
        _currentActiveId = id;
    }

    private void OnDestroy()
    {
        foreach (var box in _lpBoxDict.Values)
        {
            if (box != null)
            {
                box.OnLPClicked -= PlayLp;
            }
        }
        _LPchannel.RemoveListener<LpConncetEvent>(
            EventPlayLp);
    }
}