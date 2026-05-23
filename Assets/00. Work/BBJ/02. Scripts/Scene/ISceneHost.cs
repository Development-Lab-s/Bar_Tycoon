using BBJ.EventSystem;

namespace BBJ.Scene
{
    public interface ISceneHost
    {
        SceneType SceneType { get; }
        void OnForeground();
        void OnBackground();
    }
}
