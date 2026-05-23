using UnityEngine;
namespace BBJ.EventSystem
{
    [CreateAssetMenu(fileName = "UIManagerSO", menuName ="SO/UIManager")]
    public class UIManagerSO : ScriptableObject
    {
        public MenuType menu;
        public void OpenPopupEvent()
        {
            UIManager.Instance.OpenPopup(menu);
        }
    }

}