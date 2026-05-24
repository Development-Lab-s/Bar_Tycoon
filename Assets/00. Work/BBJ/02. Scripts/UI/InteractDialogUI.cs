using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BBJ.UI
{
    public class InteractDialogUI : MonoBehaviour, IAgentUI
    {
        public bool IsOpen { get; private set; }

        private void Awake() { gameObject.SetActive(false); }

        public UniTask OpenAsync()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            return UniTask.CompletedTask;
        }

        public UniTask CloseAsync()
        {
            gameObject.SetActive(false);
            IsOpen = false;
            return UniTask.CompletedTask;
        }
    }
}
