using _00._Work._Resources._02._Scripts.Modules;
using LitMotion;
using Systems;
using Unity.Cinemachine;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    public class CameraControl : MonoBehaviour, ICameraControl
    {
        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minSize = 1f;
        [SerializeField] private float maxSize = 7f;
        [SerializeField] private float zoomDuration = 0.2f; // 보간 시간

        [Header("Move Settings")]
        [SerializeField] private float moveDuration = 0.1f; // 이동 보간 시간
        [SerializeField] private float mouseSensitivity = 1.0f;
        [SerializeField] private float movespace = 5;

        [Header("Inputs")]
        [SerializeField] private PlayerInputSO _inputSO;

        [Header("Camera")]
        [SerializeField] private CinemachineCamera _cam;
        [SerializeField] private float mouseScale;

        private ModuleOwner _owner;
        private Vector3 _dragOrigin;
        private MotionHandle _zoomHandle;
        private MotionHandle _moveHandle;

        private bool isDragging;
        private Vector2 _lastMousePosition; // 이전 프레임의 마우스 위치 저장용
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner; 
        }
        public void AfterInit()
        {
            _inputSO.CameraMoveClick += HandleMoveStart;
        }

        private void OnDisable()
        {
            // 오브젝트 파괴/비활성화 시 실행 중인 모션 정리
            if (_zoomHandle.IsActive()) _zoomHandle.Cancel();
            if (_moveHandle.IsActive()) _moveHandle.Cancel();
        }

        void Update()
        {
            HandleZoom();
            //HandleMove();
        }

        public void HandleZoom()
        {
            float scroll = _inputSO.MouseWheel.y;
            if (scroll == 0) return;

            float targetSize = _cam.Lens.OrthographicSize - (scroll - 0.5f) * zoomSpeed;
            targetSize = Mathf.Clamp(targetSize, minSize, maxSize);

            if (_zoomHandle.IsActive()) _zoomHandle.Cancel();

            _zoomHandle = LMotion.Create(_cam.Lens.OrthographicSize, targetSize, zoomDuration)
                .WithEase(Ease.OutExpo)
                .Bind(value => _cam.Lens.OrthographicSize = value);
        }
        private Vector3 _targetCamPos;

        public void HandleMoveStart()
        {
            isDragging = true;
            _lastMousePosition = _inputSO.MousePosition;

            // ★ 핵심: 클릭한 순간, 목표 지점을 현재 카메라의 위치로 리셋 ★
            // 이렇게 해야 이전 위치에서부터 이동량이 누적되지 않고 현재 위치에서 시작합니다.
            _targetCamPos = _cam.transform.position;
        }
        public void HandleMove()
        {
            if (!_inputSO.isMouseDown)
            {
                isDragging = false;
                return;
            }

            if (isDragging)
            {
                Vector3 currentMousePos = _inputSO.MousePosition;
                Vector3 mouseDelta = currentMousePos - (Vector3)_lastMousePosition;

                if (mouseDelta.sqrMagnitude > 0.01f)
                {
                    // 1. 기본 변환 비율 계산
                    float worldScale = _cam.Lens.OrthographicSize * 2f / Screen.height;

                    Vector3 moveStep = new Vector3(
                        -mouseDelta.x * worldScale * mouseSensitivity,
                        -mouseDelta.y * worldScale * mouseSensitivity,
                        0);

                    _targetCamPos += moveStep;
                    _targetCamPos.x = Mathf.Clamp(_targetCamPos.x, -movespace, movespace);
                    _targetCamPos.y = Mathf.Clamp(_targetCamPos.y, -movespace, movespace);
                    if (_moveHandle.IsActive()) _moveHandle.Cancel();

                    _moveHandle = LMotion.Create(_cam.transform.position, _targetCamPos, moveDuration)
                        .WithEase(Ease.OutExpo)
                        .Bind(pos =>
                        {
                            pos.z = -10f;
                            _cam.transform.position = pos;
                        });
                }

                _lastMousePosition = currentMousePos;
            }
        }


    }
}