// CameraPanController.cs
// 우클릭/중클릭 드래그 + WASD/방향키 패닝
// 새 Input System 전용 (Unity 6.3 호환)

using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using BBJ.EventSystem;
using Gamelib.EventSystem;

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
        [SerializeField] private EventChannelSO sceneChannel;

        [Header("스냅 설정")]
        [SerializeField] private float panSnapThreshold = 0.01f;

        // 내부 상태 관리 변수
        private CameraBlockReason _blockReasons = CameraBlockReason.None;
        private Transform _camTransform;
        private Vector2 _targetPosition;
        private Vector3 _velocity = Vector3.zero;
        private bool _isDragging;
        private Vector2 _dragStartMousePos;
        private Vector2 _dragStartWorldPos;

        private void Awake()
        {
            if (cinemachineCamera != null) _camTransform = cinemachineCamera.transform;
            else if (mainCamera != null) _camTransform = mainCamera.transform;
            else _camTransform = transform;

            _targetPosition = _camTransform.position;

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
                _isDragging = false;
                return;
            }

            HandleMouseDrag();
            HandleKeyboardInput();
            ApplyBounds();
            ApplySmoothMove();
        }

        private void HandleMouseDrag()
        {
            if (!enableDrag) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            ButtonControl targetButton = dragButton switch
            {
                DragButton.Left => mouse.leftButton,
                DragButton.Middle => mouse.middleButton,
                DragButton.Right => mouse.rightButton,
                _ => mouse.rightButton
            };

            Vector2 mousePos = mouse.position.ReadValue();

            if (targetButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _dragStartMousePos = mousePos;
                _dragStartWorldPos = _targetPosition;
            }
            else if (targetButton.isPressed && _isDragging)
            {
                if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
                if (mainCamera == null) return;

                Vector3 startScreen = new Vector3(_dragStartMousePos.x, _dragStartMousePos.y, mainCamera.nearClipPlane);
                Vector3 currentScreen = new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane);

                Vector3 startWorld = mainCamera.ScreenToWorldPoint(startScreen);
                Vector3 currentWorld = mainCamera.ScreenToWorldPoint(currentScreen);

                Vector3 deltaWorld = currentWorld - startWorld;

                _targetPosition = _dragStartWorldPos - (Vector2)deltaWorld;
            }
            else
            {
                _isDragging = false;
            }
        }

        private void HandleKeyboardInput()
        {
            if (!enableKeyboard || _isDragging) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            Vector2 moveInput = Vector2.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1f;

            if (moveInput.sqrMagnitude > 0.001f)
            {
                float orthoSize = GetOrthoSize();
                float currentSpeed = keyboardSpeed * (orthoSize / 5f);
                _targetPosition += moveInput.normalized * (currentSpeed * Time.deltaTime);
            }
        }

        private void ApplyBounds()
        {
            if (!useBounds) return;

            _targetPosition.x = Mathf.Clamp(_targetPosition.x, minBounds.x, maxBounds.x);
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, minBounds.y, maxBounds.y);
        }

        private void ApplySmoothMove()
        {
            Vector3 current = _camTransform.position;
            Vector3 target = new Vector3(_targetPosition.x, _targetPosition.y, current.z);

            float dist = Vector2.Distance(current, target);

            if (dist < panSnapThreshold && !_isDragging)
            {
                if (current != target)
                {
                    _camTransform.position = target;
                    _velocity = Vector3.zero;
                }
                return;
            }

            _camTransform.position = Vector3.SmoothDamp(current, target, ref _velocity, smoothTime);
        }

        private float GetOrthoSize()
        {
            if (cinemachineCamera != null) return cinemachineCamera.Lens.OrthographicSize;
            if (mainCamera != null) return mainCamera.orthographicSize;
            return 5f;
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

        public void SetInputEnabled(bool isEnabled) => enableInput = isEnabled;

        public void MoveTo(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
        }
    }
}