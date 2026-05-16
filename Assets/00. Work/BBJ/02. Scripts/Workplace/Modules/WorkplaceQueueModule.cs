using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Schedule;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Modules
{
    public class WorkplaceQueueModule : MonoBehaviour, IModule
    {
        [SerializeField] private Transform[]   _queuePositions;
        [SerializeField] private EventChannelSO _scheduleChannel;

        private readonly List<OccupationSlot> _slots = new();

        public bool HasWaiting => _slots.Count > 0;

        public void Initialize(ModuleOwner owner) { }

        private void OnDisable() => _slots.Clear();

        public void Enqueue(OccupationSlot slot)
        {
            _slots.Add(slot);
            RefreshPositions();
            _scheduleChannel?.RaiseEvent(new ScheduleTriggerEvent());
        }

        public OccupationSlot? Dequeue()
        {
            if (_slots.Count == 0) return null;
            var slot = _slots[0];
            _slots.RemoveAt(0);
            RefreshPositions();
            return slot;
        }

        private void RefreshPositions()
        {
            for (int i = 0; i < _slots.Count && i < _queuePositions.Length; i++)
                _slots[i].NotifyPosition(_queuePositions[i].position);
        }
    }
}
