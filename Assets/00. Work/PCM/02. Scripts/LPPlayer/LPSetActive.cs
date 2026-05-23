using _00._Work.PCM._02._Scripts;
using UnityEngine;

public class LPSetActive : AbstructContractObject
{
    [SerializeField]private MenuType menuType;
    public void Open()
    {
        UIManager.Instance.OpenPopup(menuType);
    }
}
