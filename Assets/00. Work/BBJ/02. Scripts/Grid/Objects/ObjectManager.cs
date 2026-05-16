
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    public class ObjectManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private StageLayoutSO _stageLayout;

        [Header("Event Channels")]
        [SerializeField] private EventChannelSO _objectSpawnChannel;
        private void Awake()
        {
            UtilDebugger.AssertAllAssigned(this);
            SubEventCheannal();
        }
        private void Start()
        {
            LoadStageLayout();
        }
        private void OnDestroy()
        {
            UnSubEventCheannal();
        }


        private void LoadStageLayout()
        {
            if (_stageLayout == null) return;
            var workplaces = new List<Workplace>();
            foreach (var entry in _stageLayout.entries)
            {
                var wp = PlaceObject(entry.obstacleData, entry.cellIndex);
                if (wp != null) workplaces.Add(wp);
            }
            foreach (var wp in workplaces)
                wp.RefreshWorkPoints(_gridManager);
        }
        private Workplace PlaceObject(ObjectData data, Vector2Int cellIndex)
        {
            Vector3 worldPos = _gridManager.CellToWorld(cellIndex);
            Workplace workplace = null;

            if (data?.Prefab != null)
            {
                var go = Instantiate(data.Prefab, worldPos, Quaternion.identity);
                workplace = go.GetComponent<Workplace>();
                workplace?.SetupFromObjectData(data, cellIndex, _gridManager);
            }

            _gridManager.ApplyObstacleAt(data, cellIndex);
            workplace?.RefreshWorkPoints(_gridManager);
            return workplace;
        }

        private void SubEventCheannal()
        {
            _objectSpawnChannel?.AddListener<ObjectSpawnEvent>(HandlerSpawnObject);
        }
        private void UnSubEventCheannal()
        {
            _objectSpawnChannel?.RemoveListener<ObjectSpawnEvent>(HandlerSpawnObject);
        }
        private void HandlerSpawnObject(ObjectSpawnEvent evt) => PlaceObject(evt.ObjectData, evt.CellIndex);
    }

    public class ObjectSpawnEvent : GameEvent
    {
        public ObjectData ObjectData { get; private set; }
        public Vector2Int CellIndex { get; private set; }

        public ObjectSpawnEvent Init(ObjectData data, Vector2Int cellIndex)
        {
            ObjectData = data;
            CellIndex = cellIndex;
            return this;
        }
    }
}
