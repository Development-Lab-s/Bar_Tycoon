using BBJ.EventSystem;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Scene
{
    public class MainSceneVisibility : MonoBehaviour
    {
        [SerializeField] private EventChannelSO _sceneChannel;
        [SerializeField] private Camera         _mainCamera;
        [SerializeField] private Canvas         _mainCanvas;

        private void Awake()
        {
            _sceneChannel.AddListener<SceneTypeChangedEvent>(OnSceneChanged);
        }

        private void OnDestroy()
        {
            _sceneChannel.RemoveListener<SceneTypeChangedEvent>(OnSceneChanged);
        }

        private void OnSceneChanged(SceneTypeChangedEvent e)
        {
            bool visible = e.Current == SceneType.Main;
            if (_mainCamera) _mainCamera.gameObject.SetActive(visible);
            if (_mainCanvas) _mainCanvas.gameObject.SetActive(visible);
        }
    }
}
