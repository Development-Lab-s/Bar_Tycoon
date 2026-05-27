using _00._Work.Goat._02._Scripts.Camera;
using BBJ.EventSystem;
using Systems;
using UnityEngine;
using UnityEngine.Events;

namespace BBJ.Scene
{
    public class MainSceneHost : MonoBehaviour, ISceneHost
    {
        public SceneType SceneType => SceneType.Main;
        public Camera sceenMainCamera;
        public CameraManager cameraManager;
        [SerializeField] private PlayerInputSO playerInput;
        
        [Header("Canvas")]
        [SerializeField] private Canvas[] mainCanvases;
        [SerializeField] private Canvas storyCanvas;
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
            playerInput?.SetEnable(true);
            if (mainCanvases != null)
                foreach (var c in mainCanvases)
                    c?.gameObject.SetActive(true);
            RestoreCanvasOrders();
            storyCanvas.gameObject.SetActive(true);
        }
        public void OnBackground()
        {
            cameraManager?.gameObject.SetActive(false);
            sceenMainCamera?.gameObject.SetActive(false);
            playerInput?.SetEnable(false);
            LowerCanvasOrders();
            if (mainCanvases != null)
                foreach (var c in mainCanvases)
                    c?.gameObject.SetActive(false);
            storyCanvas.gameObject.SetActive(false);
        }
        
        private void SaveOriginalCanvasOrders()
        {
            if (mainCanvases == null) return;
            _originalOrders = new int[mainCanvases.Length];

            for (int i = 0; i < mainCanvases.Length; i++)
            {
                if (mainCanvases[i] == null) continue;
                _originalOrders[i] = mainCanvases[i].sortingOrder;
            }
        }

        private void RestoreCanvasOrders()
        {
            if (mainCanvases == null || _originalOrders == null) return;
            for (int i = 0; i < mainCanvases.Length; i++)
            {
                if (mainCanvases[i] == null) continue;

                mainCanvases[i].overrideSorting = true;
                mainCanvases[i].sortingOrder = _originalOrders[i] + foregroundOffset;
            }
        }

        private void LowerCanvasOrders()
        {
            if (mainCanvases == null || _originalOrders == null) return;
            for (int i = 0; i < mainCanvases.Length; i++)
            {
                if (mainCanvases[i] == null) continue;

                mainCanvases[i].overrideSorting = true;
                mainCanvases[i].sortingOrder = _originalOrders[i] + backgroundOffset;
            }
        }
    }
}
