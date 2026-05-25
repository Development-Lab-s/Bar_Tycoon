// FreeZoomController.cs
// SmoothDamp로 떨림 없는 부드러운 줌

using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using BBJ.EventSystem;
using BBJ.Scene;
using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Core.CameraSystems
{
    public class FreeZoomController : MonoBehaviour
    {
        [Header("카메라 참조")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private Camera mainCamera;

        [Header("줌 범위")]
        [SerializeField] private float minOrthoSize = 2f;
        [SerializeField] private float maxOrthoSize = 10f;

        [Header("휠 감도")]
        [SerializeField] private float wheelSensitivity = 1f;
        [SerializeField] private bool invertWheel = false;
        [SerializeField] private bool exponentialZoom = true;

        [Header("부드러움 (SmoothDamp)")]
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float maxSpeed = 100f;

        [Header("입력 활성화")]
        [SerializeField] private bool enableInput = true;
        [SerializeField] private EventChannelSO sceneChannel;

        [Header("정지 스냅")]
        [SerializeField] private float zoomSnapThreshold = 0.001f;

        private CameraBlockReason _blockReasons = CameraBlockReason.None;
        private float _targetOrthoSize;
        private float _currentVelocity = 0f;

        private void Awake()
        {
            _targetOrthoSize = GetCurrentOrthoSize();

            if (sceneChannel != null)
            {
                sceneChannel.AddListener<SceneTypeChangedEvent>(OnSceneTypeChanged);
            }
        }

        private void OnDestroy()
        {
            if (sceneChannel != null)
            {
                sceneChannel.RemoveListener<SceneTypeChangedEvent>(OnSceneTypeChanged);
            }
        }

        private void Update()
        {
            if (!enableInput || _blockReasons != CameraBlockReason.None)
            {
                _currentVelocity = 0f;
                return;
            }

            HandleZoomInput();
            ApplySmoothZoom();
        }

        private void HandleZoomInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            float scrollValue = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollValue) < 0.01f) return;

            float sign = invertWheel ? Mathf.Sign(scrollValue) : -Mathf.Sign(scrollValue);

            if (exponentialZoom)
            {
                float factor = 1f + (wheelSensitivity * 0.1f);
                if (sign > 0f) _targetOrthoSize *= factor;
                else _targetOrthoSize /= factor;
            }
            else
            {
                _targetOrthoSize += sign * wheelSensitivity;
            }

            _targetOrthoSize = Mathf.Clamp(_targetOrthoSize, minOrthoSize, maxOrthoSize);
        }

        private void ApplySmoothZoom()
        {
            float current = GetCurrentOrthoSize();

            if (Mathf.Abs(current - _targetOrthoSize) < zoomSnapThreshold)
            {
                if (current != _targetOrthoSize)
                {
                    SetOrthoSize(_targetOrthoSize);
                    _currentVelocity = 0f;
                }
                return;
            }

            float next = Mathf.SmoothDamp(
                current, _targetOrthoSize,
                ref _currentVelocity,
                smoothTime, maxSpeed, Time.deltaTime);

            SetOrthoSize(next);
        }

        public void SetZoom(float orthoSize, bool immediate = false)
        {
            _targetOrthoSize = Mathf.Clamp(orthoSize, minOrthoSize, maxOrthoSize);
            if (immediate)
            {
                _currentVelocity = 0f;
                SetOrthoSize(_targetOrthoSize);
            }
        }

        public void AddBlockReason(CameraBlockReason reason) => _blockReasons |= reason;
        public void RemoveBlockReason(CameraBlockReason reason) => _blockReasons &= ~reason;
        private void OnSceneTypeChanged(SceneTypeChangedEvent evt)
        {
            if (evt.Current != SceneType.Main)
            {
                AddBlockReason(CameraBlockReason.NotMainScene);
            }
            else
            {
                RemoveBlockReason(CameraBlockReason.NotMainScene);
            }
        }

        public void SetInputEnabled(bool enabled) => enableInput = enabled;
        public float CurrentOrthoSize => GetCurrentOrthoSize();

        private float GetCurrentOrthoSize()
        {
            if (cinemachineCamera != null) return cinemachineCamera.Lens.OrthographicSize;
            if (mainCamera != null) return mainCamera.orthographicSize;
            return 5f;
        }

        private void SetOrthoSize(float size)
        {
            size = Mathf.Clamp(size, minOrthoSize, maxOrthoSize);
            if (cinemachineCamera != null)
            {
                var lens = cinemachineCamera.Lens;
                lens.OrthographicSize = size;
                cinemachineCamera.Lens = lens;
                return;
            }
            if (mainCamera != null)
            {
                mainCamera.orthographicSize = size;
            }
        }
    }
}