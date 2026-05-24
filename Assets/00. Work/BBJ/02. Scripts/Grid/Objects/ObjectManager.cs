using BBJ.Save;
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BBJ.GridSystem.Objects
{
    public class ObjectManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private StageLayoutSO _stageLayout;
        [SerializeField] private ObjectDataRegistrySO _registry;

        [Header("Event Channels")]
        [SerializeField] private EventChannelSO _objectSpawnChannel;

        private readonly List<PlacedObstacleEntrySave> _placed = new();

        private void Awake() { SubEventChannel(); }
        private void OnDestroy() { UnSubEventChannel(); }

        // ─── GameLoader 호출 API ──────────────────────────────

        public void RestoreStage(StageSaveData data)
        {
            if (data == null || data.PlacedObjects.Count == 0)
            {
                LoadDefaultLayout();
                return;
            }

            _placed.Clear();
            _registry?.BuildRuntimeDict();

            var workplaces = new List<Workplace>();
            foreach (var entry in data.PlacedObjects)
            {
                var objData = _registry?.GetById(entry.ObjectDataId);
                if (objData == null) continue;

                var obj = PlaceObjectInternal(objData, entry.CellIndex, entry.FlipX);
                if (obj is Workplace wp) workplaces.Add(wp);
                _placed.Add(entry);
            }

            foreach (var wp in workplaces)
                wp.RefreshWorkPoints(_gridManager);
        }

        public void LoadDefaultLayout()
        {
            if (_stageLayout == null) return;

            _placed.Clear();
            var workplaces = new List<Workplace>();
            foreach (var entry in _stageLayout.entries)
            {
                var obj = PlaceObjectInternal(entry.obstacleData, entry.cellIndex, entry.flipX);
                if (obj is Workplace wp) workplaces.Add(wp);

                if (entry.obstacleData != null && !string.IsNullOrEmpty(entry.obstacleData.Id))
                    _placed.Add(new PlacedObstacleEntrySave
                    {
                        ObjectDataId = entry.obstacleData.Id,
                        CellIndex = entry.cellIndex,
                        FlipX = entry.flipX,
                    });
            }

            foreach (var wp in workplaces)
                wp.RefreshWorkPoints(_gridManager);
        }

        public StageSaveData GetStageSaveData()
        {
            return new StageSaveData
            {
                PlacedObjects = new List<PlacedObstacleEntrySave>(_placed),
            };
        }

        // ─── 런타임 오브젝트 배치 ────────────────────────────

        public TycoonObject PlaceObject(ObjectDataSO data, Vector2Int cellIndex, bool flipX = false)
        {
            var obj = PlaceObjectInternal(data, cellIndex, flipX);
            if (data != null && !string.IsNullOrEmpty(data.Id))
                _placed.Add(new PlacedObstacleEntrySave
                {
                    ObjectDataId = data.Id,
                    CellIndex = cellIndex,
                    FlipX = flipX,
                });
            return obj;
        }

        private TycoonObject PlaceObjectInternal(ObjectDataSO data, Vector2Int cellIndex, bool flipX)
        {
            Vector3 worldPos = _gridManager.CellToWorld(cellIndex);
            TycoonObject tycoonObject = null;

            if (data?.WorkplacePrefab != null)
            {
                var go = Instantiate(data.WorkplacePrefab, worldPos, Quaternion.identity);
                //tycoonObject = go.GetComponent<TycoonObject>();

                Func<Vector2Int, Vector3> offsetToWorld = off => _gridManager.CellToWorld(cellIndex + off);
                tycoonObject?.Setup(offsetToWorld, flipX);

                if (tycoonObject?.TileSetData != null)
                {
                    var td = tycoonObject.TileSetData;
                    _gridManager.SetNodeWalkable(cellIndex, td.IsWalkable);
                    if (td.BlockedOffsets != null)
                        foreach (var off in td.BlockedOffsets)
                        {
                            var applied = flipX ? new Vector2Int(off.y, off.x) : off;
                            _gridManager.SetNodeWalkable(cellIndex + applied, td.IsWalkable);
                        }
                }
            }

            if (tycoonObject is Workplace wp)
                wp.RefreshWorkPoints(_gridManager);

            return tycoonObject;
        }

        // ─── 이벤트 ──────────────────────────────────────────

        private void SubEventChannel()
        {
            _objectSpawnChannel?.AddListener<ObjectSpawnEvent>(HandleSpawnObject);
        }

        private void UnSubEventChannel()
        {
            _objectSpawnChannel?.RemoveListener<ObjectSpawnEvent>(HandleSpawnObject);
        }

        private void HandleSpawnObject(ObjectSpawnEvent evt)
        {
            var obj = PlaceObject(evt.ObjectData, evt.CellIndex, evt.FlipX);
            if (obj == null)
            {
                Debug.LogWarning($"[ObjectManager] SpawnEvent 처리 실패: {evt.ObjectData?.name}");
                return;
            }
            evt.OnSpawnEnded(obj.transform.position);
        }
    }

    public class ObjectSpawnEvent : GameEvent
    {
        public ObjectDataSO ObjectData { get; private set; }
        public Vector2Int CellIndex { get; private set; }
        public bool FlipX { get; private set; }
        public event Action<Vector3> CallBack;
        public void OnSpawnEnded(Vector3 pos) => CallBack?.Invoke(pos);

        public ObjectSpawnEvent Init(ObjectDataSO data, Vector2Int cellIndex, bool flipX = false, Action<Vector3> callback = default)
        {
            ObjectData = data;
            CellIndex = cellIndex;
            FlipX = flipX;
            CallBack = callback;
            return this;
        }
    }
}
