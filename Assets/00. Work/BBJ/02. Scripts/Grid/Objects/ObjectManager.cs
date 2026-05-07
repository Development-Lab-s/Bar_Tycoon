
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;
using System.Collections;
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    public class ObjectManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager   _gridManager;
        [SerializeField] private StageLayoutSO _stageLayout;

        [Header("Event Channels")]
        [SerializeField] private EventChannelSO _objectSpawnChannel;

        private void OnEnable()
            => _objectSpawnChannel?.AddListener<ObjectSpawnEvent>(OnObjectSpawnRequest);

        private void OnDisable()
            => _objectSpawnChannel?.RemoveListener<ObjectSpawnEvent>(OnObjectSpawnRequest);

        private IEnumerator Start()
        {
            yield return null; // GridManager.Awake 완료 대기
            LoadStageLayout();
        }

        private void LoadStageLayout()
        {
            if (_stageLayout == null) return;
            foreach (var entry in _stageLayout.entries)
                PlaceObject(entry.obstacleData, entry.cellIndex);
        }

        private void OnObjectSpawnRequest(ObjectSpawnEvent evt)
            => PlaceObject(evt.ObjectData, evt.CellIndex);

        private void PlaceObject(ObjectData data, Vector2Int cellIndex)
        {
            if (data?.Prefab == null) return;

            Vector3 worldPos = _gridManager.CellToWorld(cellIndex);
            var go = Instantiate(data.Prefab, worldPos, Quaternion.identity);

            _gridManager.ApplyObstacleAt(data, cellIndex);

            var workplace = go.GetComponent<Workplace>();
            workplace?.SetupFromObjectData(data, cellIndex, _gridManager);
        }
    }

    public class ObjectSpawnEvent : GameEvent
    {
        public ObjectData ObjectData { get; private set; }
        public Vector2Int CellIndex  { get; private set; }

        public ObjectSpawnEvent Init(ObjectData data, Vector2Int cellIndex)
        {
            ObjectData = data;
            CellIndex  = cellIndex;
            return this;
        }
    }
}
