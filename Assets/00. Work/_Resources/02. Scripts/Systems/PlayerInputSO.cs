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
        public event Action isClick;
        private Controls _controls;

        public Vector2 MousePosition { get; set; }
        public Vector3 Worldposition { get; set; }
        
        public void OnPointer(InputAction.CallbackContext context)
        {
            MousePosition = context.ReadValue<Vector2>();
        }
        public void OnContractClick(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                isClick?.Invoke();
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


    }
}