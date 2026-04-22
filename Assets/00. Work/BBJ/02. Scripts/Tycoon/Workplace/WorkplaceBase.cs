using BBJ.GridSystem;
using BBJ.GridSystem.Objects;
using BBJ.Register;
using BBJ.Tycoon.Data;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Tycoon.Workplaces
{
    public class WorkplaceBase : Workplace
    {
        [Header("Register")]
        [SerializeField] private WorkplaceRegisterSO _register;

        [Header("Settings")]
        [SerializeField] private WorkplaceType _workplaceType;
        public WorkplaceType WorkplaceType => _workplaceType;

        private readonly List<Vector3> _workPoints = new();
        public bool IsOccupied { get; private set; }

        private void OnEnable()  => _register.Register(this);
        private void OnDisable() => _register.Unregister(this);

        public void SetupFromObjectData(ObjectData data, Vector2Int cellIndex, GridManager gridManager)
        {
            _workPoints.Clear();

            if (data.InteractOffsets == null || data.InteractOffsets.Length == 0)
            {
                _workPoints.Add(transform.position);
                return;
            }

            foreach (var offset in data.InteractOffsets)
                _workPoints.Add(gridManager.CellToWorld(cellIndex + offset));
        }

        public void Occupy()  => IsOccupied = true;
        public void Release() => IsOccupied = false;

        public Vector3 GetNearestPoint(Vector3 from)
        {
            if (_workPoints.Count == 0) return transform.position;
            if (_workPoints.Count == 1) return _workPoints[0];

            Vector3 nearest     = _workPoints[0];
            float   nearestSqr  = Vector3.SqrMagnitude(from - nearest);

            for (int i = 1; i < _workPoints.Count; i++)
            {
                float sqr = Vector3.SqrMagnitude(from - _workPoints[i]);
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest    = _workPoints[i];
                }
            }

            return nearest;
        }
    }
}
