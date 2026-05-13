using System.Collections.Generic;
using UnityEngine;

public enum MenuType
{
    Character,
    Remodeling,
    Story,
    Collection,
    Achievement,
    Growth
}

public class MenuPopupManager : MonoBehaviour
{
    [System.Serializable]
    public class MenuPopupEntry
    {
        public MenuType menuType;
        public GameObject popupPrefab;
    }

    [SerializeField] private List<MenuPopupEntry> popupEntries;

    private Dictionary<MenuType, GameObject> popupMap;
    private GameObject currentPopup;

    void Awake()
    {
        popupMap = new Dictionary<MenuType, GameObject>();
        foreach (var entry in popupEntries)
            popupMap[entry.menuType] = entry.popupPrefab;
    }

    public void OpenPopup(MenuType type)
    {
        if (currentPopup != null)
            currentPopup.SetActive(false);

        if (popupMap.TryGetValue(type, out var prefab))
        {
            prefab.SetActive(true);
            currentPopup = prefab;
        }
    }

    public void Cancel()
    {
        gameObject.SetActive(false);
    }
}