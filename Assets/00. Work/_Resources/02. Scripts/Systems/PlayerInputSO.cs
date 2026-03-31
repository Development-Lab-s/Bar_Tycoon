using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems
{
    [CreateAssetMenu(fileName = "Player Input", menuName = "SO/Core/PlayerInput", order = 5)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public Vector2 InputDirection { get; private set; }
        
        private Controls _controls;

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