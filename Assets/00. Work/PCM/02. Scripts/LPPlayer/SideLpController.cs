using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.EventSystem;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public class SideLpController : MonoBehaviour , IModule
{
    [SerializeField] private EventChannelSO _LPchannel;
    [SerializeField] private TextMeshProUGUI text;
    private Dictionary<int, LPBOX> _lpBoxDict = new();
    private int _currentActiveId = -1;
    private int _myid = 0;
    private ModuleOwner _owner;

    // 1단계: 내 컴포넌트들을 찾고 기본적인 데이터 세팅
    public void Initialize(ModuleOwner owner)
    {
        _owner = owner;
        _LPchannel.AddListener<LpConncetEvent>(EventPlayLp);
        LPBOX[] lpBoxes = GetComponentsInChildren<LPBOX>();
        Debug.Log(lpBoxes.Length);
        for (int i = 0; i < lpBoxes.Length; i++)
        {
            // ID 등록 및 데이터 세팅
            lpBoxes[i].SetUp(i);
            _lpBoxDict.Add(i, lpBoxes[i]);
            if (i == _myid)
                PlayLp(_myid);

            // 이벤트 연결 (안전하게 기존 구독 해제 후 추가)
            lpBoxes[i].OnLPClicked -= PlayLp;
            lpBoxes[i].OnLPClicked += PlayLp;
        }
    }
    public void EventPlayLp(LpConncetEvent evt)
    {
        PlayLp(evt.Id);
    }
    public void PlayLp(int id)
    {
        Debug.Log("들어감");
        _myid = id;
        if (!_lpBoxDict.ContainsKey(id)) return;
        if (_currentActiveId == id) return;

        if (_currentActiveId != -1)
            _lpBoxDict[_currentActiveId].StopLP();

        _lpBoxDict[id].Select();
         text.text = _lpBoxDict[id].ChangeName();
        _currentActiveId = id;
    }

    private void OnDestroy()
    {
        foreach (var box in _lpBoxDict.Values)
        {
            if (box != null)
                box.OnLPClicked -= PlayLp;
        }
    }
}
