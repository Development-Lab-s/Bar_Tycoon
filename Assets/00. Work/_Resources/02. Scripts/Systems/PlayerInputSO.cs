using _00._Work.PCM._02._Scripts;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Systems
{
    [CreateAssetMenu(fileName = "Player Input",menuName = "SO/Core/PlayerInput",order = 5)]
    public class PlayerInputSO :ScriptableObject,Controls.IPlayerActions
    {
        [Header("Hover Layer")]
        [SerializeField]
        private LayerMask whatisPlayer;
        [SerializeField] private EventChannelSO soundChannel;
        // 현재 Hover 중인 대상
        private AbstructContractObject _currentHover;

        public Vector2 InputDirection { get; private set; }

        public event Action IsClick;
        public event Action CameraMoveClick;
        public event Action DownPopupClick;
        public event Action SettingPopupClick;

        private Controls _controls;
        public Vector2 MousePosition { get; private set; }
        public Vector2 MouseWheel { get; private set; }
        public bool isMouseDown { get; private set; }

        private Camera mainCam;

        public Camera MainCam
        {
            get
            {
                if (mainCam == null)
                    mainCam = Camera.main;

                return mainCam;
            }
        }

        #region Input Actions

        public void OnPointer(InputAction.CallbackContext context)
        {
            MousePosition =
                context.ReadValue<Vector2>();
        }

        public void OnContractClick(InputAction.CallbackContext context)
        { 
            if (context.performed)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;
                // 현재 Hover 대상 클릭
                _currentHover?.ExcuteClick();

                IsClick?.Invoke();
            }
        }
        public void OnIntractClick(
            InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                CameraMoveClick?.Invoke();

                isMouseDown = true;
            }

            if (context.canceled)
            {
                isMouseDown = false;
            }
        }

        public void OnScrollWheel(InputAction.CallbackContext context)
        {
            MouseWheel =
                context.ReadValue<Vector2>();
        }

        public void OnEsc(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                DownPopupClick?.Invoke();
            }
        }
        public void OnSettingEsc(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                SettingPopupClick?.Invoke();
            }
        }
        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();

                _controls.Player.SetCallbacks(this);
            }

            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
        }

        public void SetEnable(bool isEnable)
        {
            if (isEnable)
                OnEnable();
            else
                OnDisable();
        }

        #endregion
    }
}