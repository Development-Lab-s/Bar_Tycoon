using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum MenuType
{
    Character,
    LeaderBoard,
    Story,
    Collection,
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

    public GameObject CurPopup { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

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
            ClosePopup();           // 기존 팝업 끄기
            CurPopup = popup;
            CurPopup.SetActive(true); // 새 팝업 켜기 → OnEnable → OnOpen() 자동 호출
        }
    }

    public void OnOpen(GameObject popup)
    {
        menu.SetActive(false);
        ClosePopup();
        CurPopup = popup;
        CurPopup.SetActive(true);
    }

    public void ClosePopup()
    {
        if (CurPopup != null)
            CurPopup.SetActive(false);
        CurPopup = null;
    }

    public void Cancel() => menu.SetActive(false);

    [System.Serializable]
    public class MenuPopupEntry
    {
        public MenuType menuType;
        public GameObject popupPrefab;
    }
}