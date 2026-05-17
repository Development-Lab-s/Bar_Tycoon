using UnityEngine;

namespace BBJ.UI
{
    public class WorkIconUI : MonoBehaviour, IAgentUI
    {
        private void Awake() { gameObject.SetActive(false); }
        public void Open() { gameObject.SetActive(true); }
        public void Close() { gameObject.SetActive(false); }
    }
}
