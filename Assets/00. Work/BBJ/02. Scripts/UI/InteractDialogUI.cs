using UnityEngine;
using BBJ.UI;

public class InteractDialogUI : MonoBehaviour, IAgentUI
{
    private void Awake() { gameObject.SetActive(false); }
    public void OnOpen() { gameObject.SetActive(true); }
    public void OnClose() { gameObject.SetActive(false); }
}
