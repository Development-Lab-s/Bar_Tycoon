using _00._Work.Goat._02._Scripts.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Test
{
    public class TestCoin : MonoBehaviour
    {
        [SerializeField] private EventChannelSO coinChangeEvent;
        [SerializeField] private int amount;

        [ContextMenu("Test")]
        public void Test()
        {
            coinChangeEvent.RaiseEvent(new CoinEvent().Init(amount));
        }
    }
}