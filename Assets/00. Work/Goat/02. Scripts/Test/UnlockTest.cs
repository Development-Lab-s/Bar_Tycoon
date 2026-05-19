using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Test
{
    public class UnlockTest : MonoBehaviour
    {
        [SerializeField] private EventChannelSO eventChannelSO;
        [SerializeField] private ButtonType buttonType;

        [ContextMenu("Test")]
        public void Test()
        {
            eventChannelSO.RaiseEvent(new UpgradeUnLockEvent().Init(buttonType));
        }
    }
}