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
    LvUp
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    private UIManager() {}

    [SerializeField] private GameObject menu;
    [SerializeField] private List<MenuPopupEntry> popupEntries;

    private Dictionary<MenuType, GameObject> _popupMap;
    private Stack<GameObject> _popupStack = new Stack<GameObject>();

    public GameObject CurPopup => _popupStack.Count > 0 ? _popupStack.Peek() : null;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(this);
            return;
        }

        _popupMap = new Dictionary<MenuType, GameObject>();
        foreach (var entry in popupEntries)
            _popupMap[entry.menuType] = entry.popupPrefab;
    }

    private void Update()
    {
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (CurPopup == null) return;

        var popup = CurPopup.GetComponent<IPopup>();
        if (popup != null && !popup.IsAniming)
            popup.OnClose();
    }

    public void OpenPopup(MenuType type)
    {
        if (_popupMap.TryGetValue(type, out var popup))
        {
            menu.SetActive(false);
            PushPopup(popup);
        }
    }

    public void PushPopup(GameObject popup)
    {
        popup.SetActive(true);
        _popupStack.Push(popup);
    }

    public void ClosePopup()
    {
        if (_popupStack.Count == 0) return;

        var top = _popupStack.Pop();
        top.SetActive(false);
    }

    public void CloseAllPopups()
    {
        while (_popupStack.Count > 0)
            _popupStack.Pop().SetActive(false);
        menu.SetActive(true);
    }

    public void Cancel() => menu.SetActive(false);

    [System.Serializable]
    public class MenuPopupEntry
    {
        public MenuType menuType;
        public GameObject popupPrefab;
    }
}