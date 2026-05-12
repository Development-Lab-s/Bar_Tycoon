using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem.Modules
{
    public class OccupancyModule : MonoBehaviour, IModule
    {
        private WorkReservation? _currentReservation;

        public ModuleOwner CurrentOccupant { get; private set; }
        public bool IsReserved  => _currentReservation.HasValue;
        public bool IsOccupied  => CurrentOccupant != null;
        public bool IsAvailable => !IsReserved && !IsOccupied;

        public void Initialize(ModuleOwner owner) { }

        public bool TryReserve(ModuleOwner worker, Action onCancelCallback)
        {
            if (!IsAvailable) return false;
            _currentReservation = new WorkReservation(worker, onCancelCallback);
            return true;
        }

        public void Occupy(ModuleOwner worker)
        {
            if (IsReserved && _currentReservation.Value.Worker != worker)
            {
                Debug.LogWarning($"[Workplace] {worker.name}가 점유를 시도했지만, 이미 {_currentReservation.Value.Worker.name}가 예약한 자리입니다!");
                return;
            }
            _currentReservation = null;
            CurrentOccupant = worker;
        }

        public void Release()
        {
            if (IsReserved)
            {
                _currentReservation.Value.Cancel();
                _currentReservation = null;
            }
            CurrentOccupant = null;
        }
    }
}
