using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private MenuType menuType;
    [SerializeField] private bool isMenuBtn;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (UIManager.Instance.CurPopup != null && isMenuBtn)
        {
            var popup = UIManager.Instance.CurPopup.GetComponentInChildren<IPopup>(true);
        
            if (popup != null && !popup.IsAniming)
                popup.OnClose();
            
            return;
        }
    
        UIManager.Instance.OpenPopup(menuType);
    }
}