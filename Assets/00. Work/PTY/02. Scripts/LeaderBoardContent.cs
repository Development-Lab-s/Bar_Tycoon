using Gamelib.EventSystem;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LeaderBoardContent : MonoBehaviour
{
    [field: SerializeField] public EventChannelSO _eventChannel { get; set; }
    [SerializeField] private GameObject leaderBoardItem;
    [SerializeField]private LeaderBoardManager LeaderBoardManager;
    public int itemAmount = 10;
    private RectTransform _rT;
    private GridLayoutGroup _gridLayout;

    private void Awake()
    {
        _rT = GetComponent<RectTransform>();
        _gridLayout = GetComponent<GridLayoutGroup>();
        //_eventChannel.AddListener<LeaderBoardEvent>(UpdateLeaderBoardUI);
    }
    //public void UpdateLeaderBoardUI(LeaderBoardEvent evt)
    //{
    //    for (int i = 0; i < evt.Entris.Count; i++)
    //    {
    //        if (evt.Entris[i].PlayerId == evt.Id)
    //        {
    //            Debug.Log($"당신의 기록은, Score: {evt.Entris[i].Score}");
    //        }
    //        else
    //            Debug.Log($"Id:{evt.Entris[i].PlayerId}, Name:{evt.Entris[i].PlayerName}, Score: {evt.Entris[i].Score}");

    //    }
    //}
    private void OnEnable()
    {
        float newHeight = (_gridLayout.cellSize.y + _gridLayout.spacing.y) * (itemAmount - 3) + 75;

        Vector2 size = _rT.sizeDelta;
        size.y = newHeight;
        _rT.sizeDelta = size;
        _ = LeaderBoardManager.GetMyLeaderboardInfo(); 
        for (int i = 1; i <= itemAmount; i++)
        {
            if(i > 3)
            {
                GameObject item = Instantiate(leaderBoardItem, Vector3.zero, Quaternion.identity);
                item.transform.SetParent(transform, false);
                item.name = $"LeaderBoardItem {i - 3}";
                item.GetComponentInChildren<TextMeshProUGUI>().text = i.ToString();
                LeaderBoardManager.RankList.Add(item);
            }
            //LeaderBoardManager.RankList[i].
        }
    }

    private void OnDisable()
    {
        for (int i = 4; i < LeaderBoardManager.RankList.Count; i++)
        {
            var item = LeaderBoardManager.RankList[i];
            LeaderBoardManager.RankList.RemoveAt(i);
            Destroy(item);
        }
    }
}