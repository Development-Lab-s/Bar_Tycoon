using System;
using UnityEngine;
namespace BBJ.Modules
{
    public interface IPathMovement
    {
        public event Action MoveComplectedEvent;
        public void OnPathMove(Vector3[] newPath);
    }
}
