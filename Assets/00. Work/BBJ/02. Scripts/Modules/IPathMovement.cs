using System;
using UnityEngine;
namespace BBJ.Movement
{
    public interface IPathMovement
    {
        public bool IsMoving { get; }
        public Vector3 Velocity { get;}

        public event Action OnMoveStarted;
        public event Action<Vector3> OnMoveVelocityChanged;
        public event Action OnMoveCompleted;

        public void OnPathMove(Vector3[] newPath);
        public void SetMoveDestination(Vector3 destination);
        public void OnSpeedChange(float speed);
        public void StopMovement();
        public void PauseMovement();
        public void ResumeMovement();
    }
}
