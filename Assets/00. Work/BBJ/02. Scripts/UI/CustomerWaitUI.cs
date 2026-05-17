using UnityEngine;
using BBJ.UI;
using BBJ.Customer;

public class CustomerWaitUI : MonoBehaviour, IAgentUI
{
    private void Awake() { gameObject.SetActive(false); }
    public void Open() { gameObject.SetActive(true); }
    public void Close() { gameObject.SetActive(false); }

    // 고객의 현재 대기 상태에 맞게 UI 갱신 (추후 구현)
    public void Refresh(CustomerAgent customer) { }
}
