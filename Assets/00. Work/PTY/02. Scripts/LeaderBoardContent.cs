using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderBoardContent : MonoBehaviour
{
    [SerializeField] private GameObject leaderBoardItem;
    public int itemAmount = 10;
    
    private RectTransform _rT;
    private GridLayoutGroup _gridLayout;
    private List<GameObject> _items = new List<GameObject>();

    private void Awake()
    {
        _rT = GetComponent<RectTransform>();
        _gridLayout = GetComponent<GridLayoutGroup>();
    }
    
    private void OnEnable()
    {
        float newHeight = (_gridLayout.cellSize.y + _gridLayout.spacing.y) * (itemAmount - 3) + 75;

        Vector2 size = _rT.sizeDelta;
        size.y = newHeight;
        _rT.sizeDelta = size;
        
        for (int i = 4; i <= itemAmount; i++)
        {
            GameObject item = Instantiate(leaderBoardItem, Vector3.zero, Quaternion.identity);
            item.transform.SetParent(transform, false);
            item.name = $"LeaderBoardItem {i - 3}";
            item.GetComponentInChildren<TextMeshProUGUI>().text = i.ToString();
            _items.Add(item);
        }
    }

    private void OnDisable()
    {
        foreach (GameObject item in _items)
        {
            Destroy(item);
        }
        _items.Clear();
    }
}