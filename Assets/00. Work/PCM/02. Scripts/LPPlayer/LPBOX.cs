using UnityEngine;

public class LPBOX : MonoBehaviour
{
    private LP LP;
    private void Awake()
    {
        LP = GetComponentInChildren<LP>();
    }
    public void Select()
    {
        LP.Active();
    }
}
