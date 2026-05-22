using BBJ.EventSystem;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Scene
{
    // Title, Story, Cocktail 씬처럼 별도 초기화가 없는 씬에 붙여 SceneReady를 즉시 발행한다.
    public class SceneReadyTrigger : MonoBehaviour
    {
        [SerializeField] private EventChannelSO _sceneChannel;

        private void Start()
        {
            _sceneChannel.RaiseEvent(new SceneReadyEvent());
        }
    }
}
