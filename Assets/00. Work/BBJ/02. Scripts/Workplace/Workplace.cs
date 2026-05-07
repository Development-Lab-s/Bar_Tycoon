using BBJ.GridSystem;
using BBJ.GridSystem.Objects;
using BBJ.Register;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem
{
    public class Workplace : ModuleOwner
    {
        [Header("Register")]
        [SerializeField] private WorkplaceRegisterSO _register;
        [Header("Tycoon")]
        [SerializeField] private WorkplaceTypeSO _workplaceType;
        [Header("Work")]
        [SerializeField] private float _workDuration = 1f;
        public WorkplaceTypeSO WorkplaceType => _workplaceType;

        private readonly List<Vector3> _workPoints = new();
        private WorkReservation? _currentReservation = null;
        public ModuleOwner CurrentOccupant { get; private set; }
        public bool IsReserved  => _currentReservation.HasValue;
        public bool IsOccupied  => CurrentOccupant != null;
        public bool IsAvailable => !IsReserved && !IsOccupied;

        protected void OnEnable() => _register?.Register(this);
        protected void OnDisable() => _register?.Unregister(this);

        public void SetupFromObjectData(ObjectData data, Vector2Int cellIndex, GridManager gridManager)
        {
            _workPoints.Clear();

            if (data.InteractOffsets == null || data.InteractOffsets.Length == 0)
                return;

            foreach (var offset in data.InteractOffsets)
                _workPoints.Add(gridManager.CellToWorld(cellIndex + offset));
        }

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

        public event Action<float> OnProgressChanged;

        public virtual IEnumerator ExecuteWork(_00._Work._Resources._02._Scripts.Modules.ModuleOwner worker)
        {
            yield return new WaitForSeconds(_workDuration);
        }

        public virtual async UniTask ExecuteWorkAsync(ModuleOwner worker, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < _workDuration)
            {
                await UniTask.WaitForFixedUpdate(cancellationToken: ct);
                elapsed += Time.fixedDeltaTime;
                OnProgressChanged?.Invoke(elapsed / _workDuration);
            }
        }

        public Vector3 GetNearestPoint(Vector3 from)
        {
            if (_workPoints.Count == 0) return transform.position;
            if (_workPoints.Count == 1) return _workPoints[0];

            Vector3 nearest = _workPoints[0];
            float nearestDist = Vector3.SqrMagnitude(from - nearest);

            for (int i = 1; i < _workPoints.Count; i++)
            {
                float dist = Vector3.SqrMagnitude(from - _workPoints[i]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = _workPoints[i];
                }
            }

            return nearest;
        }
    }
}