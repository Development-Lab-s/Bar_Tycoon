using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum MenuType
{
    Character,
    LeaderBoard,
    Story,
    Codex,
    CocktailCodex,
    Achievement,
    Arbeit,
    Rename,
    LvUp,
    Menu,
    Order,
    StealConfirm
}

[System.Serializable]
public class MenuPopupEntry
{
    public MenuType menuType;
    public GameObject popupObject;
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject menu;
    [SerializeField] private List<MenuPopupEntry> popupEntries;

    private Dictionary<MenuType, GameObject> _popupMap;
    private readonly Stack<GameObject> _popupStack = new();

    public GameObject CurPopup
    {
        get
        {
            ClearInactivePopups();
            return _popupStack.Count > 0 ? _popupStack.Peek() : null;
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _popupMap = new Dictionary<MenuType, GameObject>();

        foreach (var entry in popupEntries)
            _popupMap[entry.menuType] = entry.popupObject;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        GameObject curPopup = CurPopup;

        if (curPopup != null)
        {
            var popup = curPopup.GetComponentInChildren<IPopup>(true);

            if (popup != null && !popup.IsAniming)
                popup.OnClose();

            return;
        }

        if (menu.activeSelf)
            menu.SetActive(false);
    }

    public void OpenPopup(MenuType type)
    {
        if (!_popupMap.TryGetValue(type, out var popup))
        {
            Debug.LogWarning($"등록되지 않은 팝업 타입입니다: {type}");
            return;
        }

        if (popup == null)
        {
            Debug.LogWarning($"팝업 오브젝트가 연결되지 않았습니다: {type}");
            return;
        }

        ClearInactivePopups();

        if (_popupStack.Contains(popup))
            return;

        menu.SetActive(false);
        PushPopup(popup);
    }

    public void PushPopup(GameObject popup)
    {
        if (popup == null)
            return;

        ClearInactivePopups();

        if (_popupStack.Contains(popup))
            return;

        popup.SetActive(true);
        _popupStack.Push(popup);
    }

    public void ClosePopup()
    {
        ClearInactivePopups();

        if (_popupStack.Count == 0)
        {
            menu.SetActive(true);
            return;
        }

        var top = _popupStack.Pop();
        top.SetActive(false);

        ClearInactivePopups();
    }

    public void CloseAllPopups()
    {
        while (_popupStack.Count > 0)
            _popupStack.Pop().SetActive(false);

        menu.SetActive(true);
    }

    private void ClearInactivePopups()
    {
        while (_popupStack.Count > 0 && !_popupStack.Peek().activeSelf)
            _popupStack.Pop();
    }
}