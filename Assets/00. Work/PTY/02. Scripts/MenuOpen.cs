using UnityEngine;

public class MenuOpen : MonoBehaviour
{
    [SerializeField] private GameObject menuPopUp;

    public void OpenPopUp()
    {
        Debug.Log("뿡");
        menuPopUp.SetActive(true);
    }
}
