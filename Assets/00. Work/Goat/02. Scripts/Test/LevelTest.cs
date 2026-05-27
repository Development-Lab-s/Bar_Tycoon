using _00._Work.Goat._02._Scripts.Events;
using Alchemy.Inspector;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Test
{
    public class LevelTest : MonoBehaviour
    {
        [SerializeField] private EventChannelSO levelChannel;
        [SerializeField] private int amount;

        [ContextMenu("Test")]
        public void Test()
        {
            levelChannel.RaiseEvent(new ExpEvent().Init(amount));
        }
        
        [Button]
        public void CoinEvent()
        {
            levelChannel.RaiseEvent(new CoinEvent().Init(amount));
        }
    }
}