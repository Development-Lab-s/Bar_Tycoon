using BBJ.Order;
using BBJ.Schedule;
using Gamelib.EventSystem;
using System;
using System.Linq;
using UnityEngine;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "WorkDispatchTable", menuName = "Tycoon/Work/DispatchTable")]
    public class WorkDispatchTableSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public OrderWorkPhase Phase;
            public OrderWorkPhase NextPhase;
            public WorkSO         Work;
        }

        [SerializeField] private Entry[] _entries;

        public Entry? FindEntry(OrderWorkPhase phase)
        {
            if (_entries == null) return null;
            return _entries.FirstOrDefault(e => e.Phase == phase);
        }

        public void Dispatch(OrderWorkPhase phase, GameEvent context, ScheduleManager scheduleManager)
        {
            if (scheduleManager == null) return;

            var entry = FindEntry(phase);
            if (entry == null) return;

            scheduleManager.Request(entry.Value.Work.RequiredRole, entry.Value.Work, context);
        }
    }
}
