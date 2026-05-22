using Gamelib.EventSystem;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LeaderBoardContent : MonoBehaviour
{
    [SerializeField] private LikeitemListSo _likeitemListSo;
    [SerializeField] private GameObject leaderBoardItem;
    [SerializeField] private LeaderBoardManager LeaderBoardManager;
    [SerializeField] private CharacterUiSO _characterUiSO;
    public List<GameObject> RankList = new List<GameObject>();
    public int itemAmount = 10;
    private RectTransform _rT;
    private GridLayoutGroup _gridLayout;
    private int loopCount = 0;

    private void Awake()
    {
        _rT = GetComponent<RectTransform>();
        _gridLayout = GetComponent<GridLayoutGroup>();
    }

    private async void OnEnable()
    {
        await LeaderBoardManager.GetMyLeaderboardInfo();
        loopCount = LeaderBoardManager.infoTuple.Count;
        LeaderBoardSetup(loopCount);
    }

    private void LeaderBoardSetup(int loop)
    {
        int dataCount = LeaderBoardManager.infoTuple.Count;
        loopCount = Mathf.Min(itemAmount, dataCount);

        float newHeight = (_gridLayout.cellSize.y + _gridLayout.spacing.y) * (loopCount - 3) + 75;
        Vector2 size = _rT.sizeDelta;
        size.y = newHeight;
        _rT.sizeDelta = size;

        for (int i = 1; i <= loopCount; i++)
        {
            GameObject item = null;

            if (i > 3)
            {
                item = Instantiate(leaderBoardItem, Vector3.zero, Quaternion.identity);
                item.transform.SetParent(transform, false);
                item.name = $"LeaderBoardItem {i - 3}";
                item.GetComponent<LeaderBoardInfo>().LBRank.text = i.ToString();
                RankList.Add(item);
            }
            else
            {
                if (RankList.Count >= i)
                {
                    item = RankList[i - 1];
                }
            }

            if (item != null)
            {
                LeaderBoardInfo info = item.GetComponent<LeaderBoardInfo>();
                var data = LeaderBoardManager.infoTuple[i - 1];

                info.LBName.text = data.Name;
                info.LBPlayerLv.text ="Lv "+ data.Level.ToString();
                info.GoldAmount.text = data.Gold.ToString();
                info.Image.sprite = _characterUiSO.GetSprite(data.FavoriteCharacter);
            }
        }
    }

    private void OnDisable()
    {
        for (int i = RankList.Count - 1; i >= 3; i--)
        {
            GameObject item = RankList[i];
            RankList.RemoveAt(i);
            if (item != null)
            {
                Destroy(item);
            }
        }
        LeaderBoardManager.infoTuple.Clear();
    }
}