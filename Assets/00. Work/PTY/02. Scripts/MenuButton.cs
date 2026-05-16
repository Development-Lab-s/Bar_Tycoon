using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private MenuType menuType;
    
    // Inspector에서 MenuPopupManager 연결하거나
    private MenuPopupManager popupManager;

    void Start()
    {
        popupManager = FindAnyObjectByType<MenuPopupManager>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        popupManager.OpenPopup(menuType);
    }
}