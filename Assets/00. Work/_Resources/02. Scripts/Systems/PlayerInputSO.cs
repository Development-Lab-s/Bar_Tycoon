using _00._Work.PCM._02._Scripts;
using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems
{
    [CreateAssetMenu(fileName = "Player Input", menuName = "SO/Core/PlayerInput", order = 5)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        [SerializeField]private LayerMask whatisPlayer;
        public Vector2 InputDirection { get; private set; }
        public event Action IsClick;
        public event Action CameraMoveClick;
        public event Action DownPopupClick;
        public event Action SettingPopupClick;
        private Controls _controls;

        public Vector2 MousePosition { get; set; }
        public Vector2 MouseWheel { get; set; }

        public bool isMouseDown { get; set; }

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

        public void OnPointer(InputAction.CallbackContext context)
        {
            MousePosition = context.ReadValue<Vector2>();
        }
        public void OnContractClick(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                IsClick?.Invoke();
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

        public void OnScrollWheel(InputAction.CallbackContext context)
        {
           MouseWheel = context.ReadValue<Vector2>();
        }

        public void OnIntractClick(InputAction.CallbackContext context)
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
    }
}