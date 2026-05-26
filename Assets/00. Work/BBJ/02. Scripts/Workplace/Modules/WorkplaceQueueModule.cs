using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Schedule;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Modules
{
    public class WorkplaceQueueModule : MonoBehaviour, IModule
    {
        [SerializeField] private Transform[]    _queuePositions;
        [SerializeField] private float          _spacing = 1f;
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

        public bool TryCompleteSlotByOwner(Transform owner)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Owner != owner) continue;
                var slot = _slots[i];
                _slots.RemoveAt(i);
                RefreshPositions();
                slot.NotifyProcessed();
                return true;
            }
            return false;
        }

        private void RefreshPositions()
        {
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].NotifyPosition(GetQueuePosition(i));
        }

        private Vector3 GetQueuePosition(int index)
        {
            if (_queuePositions == null || _queuePositions.Length == 0)
                return transform.position;

            if (index < _queuePositions.Length)
                return _queuePositions[index].position;

            // 정의된 포인트 초과 시 마지막에서 같은 방향으로 연장
            int last = _queuePositions.Length - 1;
            Vector3 dir = _queuePositions.Length >= 2
                ? (_queuePositions[last].position - _queuePositions[last - 1].position).normalized
                : Vector3.back;

            return _queuePositions[last].position + dir * (_spacing * (index - last));
        }
    }
}
