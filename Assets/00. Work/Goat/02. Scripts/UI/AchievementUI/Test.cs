using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.UI.AchievementUI.Data;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI
{
    public class Test : MonoBehaviour
    {
        [SerializeField] private EventChannelSO eventChannel;
        [ContextMenu("Refresh")]
        public void Tester()
        {
            eventChannel.RaiseEvent(new AchievementEvent().Init(AchievementType.Test, 1));
        }
    }
}