public class RemodelingPopup : BasePopup
{
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

    private void Start()
    {
        OnOpen();
    }
}