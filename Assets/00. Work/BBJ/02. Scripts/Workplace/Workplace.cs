using BBJ.GridSystem;
using BBJ.GridSystem.Objects;
using BBJ.Register;
using System.Collections.Generic;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.GridSystem.Pathfind;

namespace BBJ.WorkplaceSystem
{
    public class Workplace : ModuleOwner
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO _workplaceType;
        public WorkplaceTypeSO WorkplaceType => _workplaceType;

        private List<Vector3> _workPoints = new();
        public bool HasWorkPoints => _validWorkPoints.Count > 0;
        private List<Vector3> _validWorkPoints = new();

        protected override void Awake()
        {
            base.Awake();
            _register?.Register(this);
        }
        private void OnDestroy()
        {
            _register?.Unregister(this);
        }



        public void SetupFromObjectData(ObjectData data, Vector2Int cellIndex, GridManager gridManager)
        {
            _workPoints.Clear();
            _validWorkPoints.Clear();

            if (data.InteractOffsets == null || data.InteractOffsets.Length == 0) return;

            foreach (var offset in data.InteractOffsets)
                _workPoints.Add(gridManager.CellToWorld(cellIndex + offset));

        }

        public void RefreshWorkPoints(GridManager gridManager)
        {
            _validWorkPoints.Clear();
            foreach (var point in _workPoints)
            {
                Node node = gridManager.NodeFromWorldPoint(point);
                if (node != null && node.walkable)
                    _validWorkPoints.Add(point);
            }
        }

        public Vector3 GetNearestPoint(Vector3 from)
        {
            if (_validWorkPoints.Count == 0)
            {
                return transform.position;
            }

            Vector3 nearest = _validWorkPoints[0];
            float nearestDist = Vector3.SqrMagnitude(from - nearest);

            for (int i = 1; i < _validWorkPoints.Count; i++)
            {
                float dist = Vector3.SqrMagnitude(from - _validWorkPoints[i]);
                if (dist < nearestDist) { nearestDist = dist; nearest = _validWorkPoints[i]; }
            }
            return nearest;
        }

    }

}
