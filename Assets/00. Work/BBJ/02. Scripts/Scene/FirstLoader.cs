using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Scene
{
    [DefaultExecutionOrder(-20)]
    public class FirstLoader : MonoBehaviour
    {
        [SerializeField] private EventChannelSO[] _channelsToClear;

        private void Awake()
        {
            Time.timeScale = 1.0f;
            foreach (var ch in _channelsToClear)
                ch?.Clear();
        }
    }
}
