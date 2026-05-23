using BBJ.EventSystem;
using UnityEngine;

namespace BBJ.Scene
{
    public class TitleSceneHost : MonoBehaviour, ISceneHost
    {
        public SceneType SceneType => SceneType.Title;

        private void Awake() => GameSceneManager.Instance.RegisterHost(this);

        public void OnForeground() { }
        public void OnBackground() { }
    }
}
