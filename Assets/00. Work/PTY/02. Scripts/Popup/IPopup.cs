public interface IPopup
{
    public bool IsAniming { get; }
    
    public void OnClickClose()
    {
        
    }

    public void OnClose();
}