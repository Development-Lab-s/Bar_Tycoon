// FreeZoomController.cs
// SmoothDamp로 떨림 없는 부드러운 줌

using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

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
        // 지수 줌: 휠 한 칸당 "비율"로 변화 → 멀리서는 빠르게, 가까이서는 천천히

        [Header("부드러움 (SmoothDamp)")]
        [SerializeField] private float smoothTime = 0.15f;  // 0.1~0.3 사이 추천
        [SerializeField] private float maxSpeed = 100f;

        [Header("입력 활성화")]
        [SerializeField] private bool enableInput = true;
        
        [Header("정지 스냅")]
        [SerializeField] private float zoomSnapThreshold = 0.001f;

        private float _targetOrthoSize;
        private float _currentVelocity; // SmoothDamp 내부 상태

        private void Start()
        {
            _targetOrthoSize = GetCurrentOrthoSize();
        }

        private void Update()
        {
            if (enableInput) HandleWheelInput();
            ApplySmoothZoom();
        }

        private void HandleWheelInput()
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f)) return;
            if (invertWheel) scroll = -scroll;

            if (exponentialZoom)
            {
                // 비율 기반: 줌 레벨에 비례한 변화량
                float factor = Mathf.Pow(0.9f, scroll * wheelSensitivity);
                _targetOrthoSize *= factor;
            }
            else
            {
                _targetOrthoSize -= scroll * wheelSensitivity;
            }

            _targetOrthoSize = Mathf.Clamp(_targetOrthoSize, minOrthoSize, maxOrthoSize);
        }

        // 목표값과의 차이가 이 값 이하면 즉시 스냅하고 업데이트 중단
        private void ApplySmoothZoom()
        {
            float current = GetCurrentOrthoSize();
            float diff = Mathf.Abs(current - _targetOrthoSize);

            // 충분히 가까우면 정지 — 미세 떨림 차단
            if (diff < zoomSnapThreshold)
            {
                if (!Mathf.Approximately(current, _targetOrthoSize))
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
            if (mainCamera != null) mainCamera.orthographicSize = size;
        }
    }
}