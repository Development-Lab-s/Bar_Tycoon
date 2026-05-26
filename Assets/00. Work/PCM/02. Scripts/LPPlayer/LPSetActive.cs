using _00._Work.PCM._02._Scripts;
using Assets._00._Work.PCM._02._Scripts;
using UnityEngine;

public class LPSetActive : TestContractObject
{
    [SerializeField]private MenuType menuType;
    public void Open()
    {
        UIManager.Instance.OpenPopup(menuType);
    }
}
