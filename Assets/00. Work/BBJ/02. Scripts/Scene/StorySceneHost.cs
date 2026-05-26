using BBJ.EventSystem;
using Gamelib.EventSystem;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using UnityEngine;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;

namespace BBJ.Scene
{
    public class StorySceneHost : MonoBehaviour, ISceneHost
    {
        public SceneType SceneType => SceneType.Story;

        private void Awake() => GameSceneManager.Instance.RegisterHost(this);

        public void OnForeground() { }
        public void OnBackground() { }
    }
}