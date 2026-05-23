using System.Collections.Generic;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Events
{
    public class CameraManagerEvent : GameEvent
    {
        public List<Vector2> objectPositionList = new List<Vector2>();
        public CameraManagerEvent Init(List<Vector2> objectPositions)
        {
            objectPositionList = objectPositions;
            return this;
        }
    }
}