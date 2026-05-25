using _00._Work.Goat._02._Scripts.Camera;
using BBJ.EventSystem;
using UnityEngine;

namespace BBJ.Scene
{
    public class MainSceneHost : MonoBehaviour, ISceneHost
    {
        public SceneType SceneType => SceneType.Main;
        public Camera sceenMainCamera;
        public CameraManager cameraManager;

        private void Awake()
        {
            GameSceneManager.Instance.RegisterHost(this);

            cameraManager = FindFirstObjectByType<CameraManager>();
            sceenMainCamera?.gameObject.SetActive(false);
            cameraManager?.gameObject.SetActive(false);
        }

        public void OnForeground()
        {
            sceenMainCamera?.gameObject.SetActive(true);
            cameraManager?.gameObject.SetActive(true);
            Camera.SetupCurrent(sceenMainCamera);
        }
        public void OnBackground()
        {
            cameraManager?.gameObject.SetActive(false);
            sceenMainCamera?.gameObject.SetActive(false);
        }
    }
}
