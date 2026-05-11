using BBJ.GridSystem;
using BBJ.GridSystem.Objects;
using BBJ.Register;
using System.Collections.Generic;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem
{
    public class Workplace : ModuleOwner
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO _workplaceType;
        public WorkplaceTypeSO WorkplaceType => _workplaceType;

        private readonly List<Vector3> _workPoints = new();

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
