
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;
using System.Collections;
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

        private void OnEnable()
        {
            SubEventCheannal();
        }
        private void OnDisable()
        {
            UnSubEventCheannal();
        }

        private void Start()
        {
            LoadStageLayout();
        }

        private void LoadStageLayout()
        {
            if (_stageLayout == null) return;
            foreach (var entry in _stageLayout.entries)
                PlaceObject(entry.obstacleData, entry.cellIndex);
        }

        private void PlaceObject(ObjectData data, Vector2Int cellIndex)
        {
            Vector3 worldPos = _gridManager.CellToWorld(cellIndex);

            if (data?.Prefab != null)
            {
                var go = Instantiate(data.Prefab, worldPos, Quaternion.identity);
                go.GetComponent<Workplace>()?
                    .SetupFromObjectData(data, cellIndex, _gridManager);
            }

            _gridManager.ApplyObstacleAt(data, cellIndex);
        }
        private void ObjectSpawnHandler(ObjectSpawnEvent evt) => PlaceObject(evt.ObjectData, evt.CellIndex);

        private void SubEventCheannal()
        {
            _objectSpawnChannel?.AddListener<ObjectSpawnEvent>(ObjectSpawnHandler);
        }
        private void UnSubEventCheannal()
        {
            _objectSpawnChannel?.RemoveListener<ObjectSpawnEvent>(ObjectSpawnHandler);
        }
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
