using UnityEngine;
using BBJ.UI;

public class InteractDialogUI : MonoBehaviour, IAgentUI
{
    private void Awake() { gameObject.SetActive(false); }
    public void Open() { gameObject.SetActive(true); }
    public void Close() { gameObject.SetActive(false); }
}
