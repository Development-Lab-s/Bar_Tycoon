using UnityEngine;

public class CharacterPopup : BasePopup
{
    void Start()
    {   
        OnOpen();
    }
    
    public override void OnClickClose()
    {
        OnClose();
        gameObject.SetActive(false);
    }
    
    protected override void OnOpen()
    {
        
    }

    protected override void OnClose()
    {
        
    }
}
