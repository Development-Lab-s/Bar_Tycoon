using BBJ.EventSystem;
using UnityEngine;

namespace BBJ.Scene
{
    public class MainSceneHost : MonoBehaviour, ISceneHost
    {
        public SceneType SceneType => SceneType.Main;

        private void Awake() => GameSceneManager.Instance.RegisterHost(this);

        public void OnForeground() { }
        public void OnBackground() { }
    }
}
