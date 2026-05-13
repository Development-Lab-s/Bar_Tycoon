using UnityEngine;

public abstract class BasePopup : MonoBehaviour
{
    public virtual void OnClickClose()
    {
        
    }

    protected abstract void OnOpen();

    protected abstract void OnClose();
}