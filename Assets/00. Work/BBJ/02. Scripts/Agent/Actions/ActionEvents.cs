using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;
using System;
using UnityEngine;

namespace BBJ.Actions
{
    public class MoveEvent : GameEvent
    {
        public readonly Vector3 Destination;
        public MoveEvent(Vector3 destination)
        {
            this.Destination = destination;
        }
    }
    public class WaitEvent : GameEvent
    {
        public float Seconds { get; }
        public Func<bool> Condition { get; }

        public WaitEvent(float seconds) => Seconds = seconds;
        public WaitEvent(Func<bool> condition) => Condition = condition;
    }

    public class WorkEvent : GameEvent
    {
        public Workplace Workplace { get; }
        public WorkEvent(Workplace workplace) => Workplace = workplace;
    }
}
