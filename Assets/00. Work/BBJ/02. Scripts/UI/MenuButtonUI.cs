using UnityEngine;
using UnityEngine.EventSystems;

namespace BBJ.UI
{
    public class MenuButtonUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private MenuType _menuType;

        public void OnPointerClick(PointerEventData eventData)
        {
            UIManager.Instance.OpenPopup(_menuType);
        }
    }
}
