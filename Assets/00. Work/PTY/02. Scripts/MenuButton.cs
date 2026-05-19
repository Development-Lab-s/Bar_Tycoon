using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private MenuType menuType;

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.Instance.OpenPopup(menuType);
    }
}