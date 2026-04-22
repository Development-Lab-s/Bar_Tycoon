using BBJ.GridSystem;
using BBJ.GridSystem.Objects;
using BBJ.Register;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.Tycoon
{
    public class Workplace : MonoBehaviour
    {
        [Header("Register")]
        [SerializeField] private WorkplaceRegisterSO _register;

        [Header("Tycoon")]
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
            float   nearestDist = Vector3.SqrMagnitude(from - nearest); // sqrt 생략

            for (int i = 1; i < _workPoints.Count; i++)
            {
                float dist = Vector3.SqrMagnitude(from - _workPoints[i]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest     = _workPoints[i];
                }
            }

            return nearest;
        }
    }
}
