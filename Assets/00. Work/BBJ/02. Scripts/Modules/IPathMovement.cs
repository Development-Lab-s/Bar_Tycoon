using System;
using UnityEngine;
namespace BBJ.Movement
{
    public interface IPathMovement
    {
        public event Action OnMoveCompleted;
        public void OnPathMove(Vector3[] newPath);
        void StopMovement();
    }
}
