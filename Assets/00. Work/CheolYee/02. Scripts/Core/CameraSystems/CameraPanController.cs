// CameraPanController.cs
// 우클릭/중클릭 드래그 + WASD/방향키 패닝
// 새 Input System 전용 (Unity 6.3 호환)

using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace _00._Work.CheolYee._02._Scripts.Core.CameraSystems
{
    public class CameraPanController : MonoBehaviour
    {
        private enum DragButton { Left, Right, Middle }

        [Header("카메라 참조")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private Camera mainCamera;

        [Header("드래그 설정")]
        [SerializeField] private bool enableDrag = true;
        [SerializeField] private DragButton dragButton = DragButton.Right;

        [Header("키보드 설정")]
        [SerializeField] private bool enableKeyboard = true;
        [SerializeField] private float keyboardSpeed = 5f;

        [Header("이동 한계 (월드 좌표)")]
        [SerializeField] private bool useBounds;
        [SerializeField] private Vector2 minBounds = new(-50, -50);
        [SerializeField] private Vector2 maxBounds = new(50, 50);

        [Header("부드러움")]
        [SerializeField] private float smoothTime = 0.1f;

        [Header("입력 활성화")]
        [SerializeField] private bool enableInput = true;
        
        [Header("정지 스냅")]
        [SerializeField] private float panSnapThreshold = 0.002f;

        private Transform _camTransform;
        private Vector3 _targetPosition;
        private Vector3 _velocity;
        private bool _isDragging;

        private void Awake()
        {
            _camTransform = cinemachineCamera != null
                ? cinemachineCamera.transform
                : (mainCamera != null ? mainCamera.transform : transform);
            _targetPosition = _camTransform.position;
        }

        private void Update()
        {
            if (enableInput)
            {
                if (enableDrag) HandleDrag();
                if (enableKeyboard) HandleKeyboard();
            }
            ApplyClamp();
            ApplySmoothMove();
        }

        // ── 드래그 ─────────────────────────────────────────

        private void HandleDrag()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            ButtonControl button = GetDragButton(mouse);

            if (button.wasPressedThisFrame)
                _isDragging = true;
            else if (button.wasReleasedThisFrame)
                _isDragging = false;

            if (!_isDragging) return;

            // 한 프레임 동안 마우스가 픽셀 단위로 얼마나 이동했는지
            Vector2 mouseDelta = mouse.delta.ReadValue();
            if (mouseDelta.sqrMagnitude < 0.0001f) return;

            // 스크린 1픽셀 = 월드 몇 단위? (정직한 변환식, 줌 레벨에 자동 비례)
            float worldPerPixel = (GetOrthoSize() * 2f) / Screen.height;

            // 마우스가 오른쪽으로 가면 화면이 왼쪽으로 가야 손에 붙어 따라옴
            _targetPosition.x -= mouseDelta.x * worldPerPixel;
            _targetPosition.y -= mouseDelta.y * worldPerPixel;
        }

        private ButtonControl GetDragButton(Mouse mouse)
        {
            return dragButton switch
            {
                DragButton.Left   => mouse.leftButton,
                DragButton.Right  => mouse.rightButton,
                DragButton.Middle => mouse.middleButton,
                _                 => mouse.rightButton,
            };
        }

        // ── 키보드 ─────────────────────────────────────────

        private void HandleKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float h = 0f, v = 0f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;

            if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f)) return;

            Vector3 dir = new Vector3(h, v, 0f).normalized;
            float zoomFactor = GetOrthoSize() / 5f; // 확대 시 천천히
            _targetPosition += dir * (keyboardSpeed * zoomFactor * Time.deltaTime);
        }

        // ── 적용 ──────────────────────────────────────────

        private void ApplyClamp()
        {
            if (!useBounds) return;
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, minBounds.x, maxBounds.x);
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, minBounds.y, maxBounds.y);
        }
        
        // 거리(월드 단위)가 이 값 이하면 정지

        private void ApplySmoothMove()
        {
            Vector3 current = _camTransform.position;
            Vector3 target = new Vector3(_targetPosition.x, _targetPosition.y, current.z);

            float dist = Vector2.Distance(current, target);

            // 정지 — 미세 떨림 차단
            if (dist < panSnapThreshold && !_isDragging)
            {
                if (current != target)
                {
                    _camTransform.position = target;
                    _velocity = Vector3.zero;
                }
                return;
            }

            _camTransform.position = Vector3.SmoothDamp(
                current, target, ref _velocity, smoothTime);
        }

        // ── 유틸 ──────────────────────────────────────────

        private float GetOrthoSize()
        {
            if (cinemachineCamera != null) return cinemachineCamera.Lens.OrthographicSize;
            if (mainCamera != null) return mainCamera.orthographicSize;
            return 5f;
        }

        // ── 외부 API ──────────────────────────────────────

        public void SetInputEnabled(bool isEnabled) => enableInput = isEnabled;
        public void MoveTo(Vector2 worldPos) =>
            _targetPosition = new Vector3(worldPos.x, worldPos.y, _camTransform.position.z);
    }
}