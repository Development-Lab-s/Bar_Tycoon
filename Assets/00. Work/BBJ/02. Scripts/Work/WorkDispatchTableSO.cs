using BBJ.Order;
using Gamelib.EventSystem;
using System;
using System.Collections.Generic;
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

        [SerializeField] private Entry[]        _entries;
        [SerializeField] private EventChannelSO _scheduleChannel;

        private Dictionary<OrderWorkPhase, Entry> _entryDic = new();

        private void Awake()
        {
            _entryDic = _entries.ToDictionary(e => e.Phase);
        }

        public void OnDisable() => _entryDic.Clear();

        public Entry? FindEntry(OrderWorkPhase phase)
        {
            if (_entries == null) return null;
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i].Phase == phase) return _entries[i];
            return null;
        }

        public void Dispatch(OrderWorkPhase phase, OrderTicket ticket)
        {
            var entry = FindEntry(phase);
            if (entry == null || entry.Value.Work == null) return;
            _scheduleChannel?.RaiseEvent(
                new ScheduleRequestEvent(entry.Value.Work.RequiredRole, entry.Value.Work, ticket));
        }
    }
}
