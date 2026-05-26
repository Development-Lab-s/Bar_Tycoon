using _00._Work.Goat._02._Scripts.Camera;
using BBJ.EventSystem;
using UnityEngine;
using UnityEngine.Events;

namespace BBJ.Scene
{
    public class MainSceneHost : MonoBehaviour, ISceneHost
    {
        public SceneType SceneType => SceneType.Main;
        public Camera sceenMainCamera;
        public CameraManager cameraManager;
        
        [Header("Canvas")]
        [SerializeField] private Canvas[] mainCanvases;
        [SerializeField] private int foregroundOffset = 0;
        [SerializeField] private int backgroundOffset = -1000;
        public UnityEvent tutorialStart;
        private int[] _originalOrders;

        private void Awake()
        {
            GameSceneManager.Instance.RegisterHost(this);

            cameraManager = FindFirstObjectByType<CameraManager>();
            sceenMainCamera?.gameObject.SetActive(false);
            cameraManager?.gameObject.SetActive(false);
            
            SaveOriginalCanvasOrders();
        }

        public void OnForeground()
        {
            sceenMainCamera?.gameObject.SetActive(true);
            cameraManager?.gameObject.SetActive(true);
            tutorialStart?.Invoke();
            Camera.SetupCurrent(sceenMainCamera);
            
            RestoreCanvasOrders();
        }
        public void OnBackground()
        {
            cameraManager?.gameObject.SetActive(false);
            sceenMainCamera?.gameObject.SetActive(false);
            
            LowerCanvasOrders();
        }
        
        private void SaveOriginalCanvasOrders()
        {
            _originalOrders = new int[mainCanvases.Length];

            for (int i = 0; i < mainCanvases.Length; i++)
            {
                if (mainCanvases[i] == null) continue;
                _originalOrders[i] = mainCanvases[i].sortingOrder;
            }
        }

        private void RestoreCanvasOrders()
        {
            for (int i = 0; i < mainCanvases.Length; i++)
            {
                if (mainCanvases[i] == null) continue;

                mainCanvases[i].overrideSorting = true;
                mainCanvases[i].sortingOrder = _originalOrders[i] + foregroundOffset;
            }
        }

        private void LowerCanvasOrders()
        {
            for (int i = 0; i < mainCanvases.Length; i++)
            {
                if (mainCanvases[i] == null) continue;

                mainCanvases[i].overrideSorting = true;
                mainCanvases[i].sortingOrder = _originalOrders[i] + backgroundOffset;
            }
        }
    }
}
