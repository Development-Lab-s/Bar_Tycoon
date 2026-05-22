using BBJ.GridSystem;
using BBJ.GridSystem.Objects;
using BBJ.Register;
using System;
using System.Collections.Generic;
using UnityEngine;
using BBJ.GridSystem.Pathfind;

namespace BBJ.WorkplaceSystem
{
    public class Workplace : TycoonObject
    {
        [SerializeField] private WorkplaceRegisterSO  _register;
        [SerializeField] private WorkplaceTypeSO      _workplaceType;
        public WorkplaceTypeSO WorkplaceType => _workplaceType;

        [SerializeField] private List<InteractPoint>  _interactPoints = new();

        private readonly Dictionary<InteractRoleSO, List<Vector3>> _validPoints  = new();

        protected override void Awake()
        {
            base.Awake();
            _register?.Register(this);
        }

        private void OnDestroy()
        {
            _register?.Unregister(this);
        }

        public override void Setup(Vector2Int cellIndex, Func<Vector2Int, Vector3> cellToWorld, bool flipX)
        {
            base.Setup(cellIndex, cellToWorld, flipX);
            _validPoints.Clear();

            foreach (var ip in _interactPoints)
            {
                if (ip.Role == null) continue;

                var offset   = flipX ? new Vector2Int(-ip.Offset.x, ip.Offset.y) : ip.Offset;
                var worldPos = cellToWorld(cellIndex + offset);

                if (!_validPoints.TryGetValue(ip.Role, out var list))
                {
                    list = new List<Vector3>();
                    _validPoints[ip.Role] = list;
                }
                list.Add(worldPos);
            }
        }

        public void RefreshWorkPoints(GridManager gridManager)
        {
            foreach (var kvp in _validPoints)
            {
                var filtered = new List<Vector3>();
                foreach (var pt in kvp.Value)
                {
                    Node node = gridManager.NodeFromWorldPoint(pt);
                    if (node != null && node.walkable)
                        filtered.Add(pt);
                }
                _validPoints[kvp.Key] = filtered;
            }
        }

        public Vector3 GetNearestPoint(InteractRoleSO role, Vector3 from)
        {
            if (role != null && _validPoints.TryGetValue(role, out var points) && points.Count > 0)
                return GetNearestFrom(points, from);

            Debug.LogWarning($"[Workplace] {name}: role '{(role != null ? role.name : "null")}' 에 해당하는 InteractPoint 없음. transform.position 반환.", this);
            return transform.position;
        }

        private static Vector3 GetNearestFrom(List<Vector3> points, Vector3 from)
        {
            Vector3 nearest     = points[0];
            float   nearestDist = Vector3.SqrMagnitude(from - nearest);

            for (int i = 1; i < points.Count; i++)
            {
                float dist = Vector3.SqrMagnitude(from - points[i]);
                if (dist < nearestDist) { nearestDist = dist; nearest = points[i]; }
            }
            return nearest;
        }
    }
}
