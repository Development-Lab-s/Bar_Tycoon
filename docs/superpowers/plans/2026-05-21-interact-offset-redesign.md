# InteractOffset 재설계 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `TileSetData`에서 `InteractOffsets`/`StaffInteractOffsets`를 제거하고, `InteractRoleSO` 기반의 `InteractPoint` 구조로 교체하여 역할별 인터랙션 포인트를 `Workplace` 컴포넌트에서 직접 관리한다.

**Architecture:** `InteractRoleSO`(SO 참조 비교로 역할 식별) + `InteractPoint` struct(`Offset + Role`) → `Workplace._interactPoints`(프리팹에 직렬화) → `SchedulingModule._interactRole`(에이전트 단위 역할 보유) → `WorkSO`가 실행자의 role을 꺼내 `workplace.GetNearestPoint(role, from)` 호출.

**Tech Stack:** Unity 2022+, C#, UGUI, Unity Editor Custom Editor/Handles

---

## 파일 맵

| 상태 | 경로 | 변경 내용 |
|------|------|-----------|
| 신규 | `Assets/00. Work/BBJ/02. Scripts/Workplace/InteractRoleSO.cs` | 역할 식별 SO (GizmoColor) |
| 신규 | `Assets/00. Work/BBJ/02. Scripts/Workplace/InteractPoint.cs` | Offset + Role struct |
| 신규 | `Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceEditor.cs` | Gizmo + 인스펙터 Custom Editor |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/TileSetData.cs` | Prefab/InteractOffsets/StaffInteractOffsets/IsInteractable/Id 제거 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/ObjectDataSO.cs` | Id(GUID)/WorkplacePrefab 추가, TileSet 제거 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/ObjectDataRegistrySO.cs` | ObjectDataSO 기준으로 인덱싱 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/PlacedObstacleEntry.cs` | TileSetData→ObjectDataSO, FlipX 추가 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Grid/GridManager.cs` | ApplyObstacleAt에 flipX 파라미터 추가 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Workplace/Workplace.cs` | _interactPoints/_tileSetData/_flipX, Setup 시그니처 변경, GetNearestPoint(role) |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs` | InteractRoleSO _interactRole 추가 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Save/GameSaveData.cs` | PlacedObstacleEntrySave.FlipX 추가 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/ObjectManager.cs` | PlaceObject API → ObjectDataSO, FlipX |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs` | GetNearestPoint(role) 호출 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs` | GetNearestPoint(role) 호출 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/ExitWorkSO.cs` | GetNearestPoint(role) 호출 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/TakeSeatWorkSO.cs` | GetNearestPoint(role) 호출 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs` | GetNearestPoint(role) 호출 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/PayAtCounterWorkSO.cs` | GetNearestPoint(role) 호출 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs` | GetNearestPoint(role) 호출 ×2 |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/Editor/ObstacleDataEditor.cs` | InteractOffset 관련 UI 제거, Blocked 전용으로 축소 |

---

## Task 1: InteractRoleSO + InteractPoint 생성

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Workplace/InteractRoleSO.cs`
- Create: `Assets/00. Work/BBJ/02. Scripts/Workplace/InteractPoint.cs`

- [ ] **Step 1: InteractRoleSO.cs 작성**

```csharp
using UnityEngine;

namespace BBJ.WorkplaceSystem
{
    [CreateAssetMenu(fileName = "InteractRole", menuName = "Tycoon/Workplace/InteractRole")]
    public class InteractRoleSO : ScriptableObject
    {
        public Color GizmoColor = Color.cyan;
    }
}
```

- [ ] **Step 2: InteractPoint.cs 작성**

```csharp
using System;
using UnityEngine;

namespace BBJ.WorkplaceSystem
{
    [Serializable]
    public struct InteractPoint
    {
        public Vector2Int Offset;
        public InteractRoleSO Role;
    }
}
```

- [ ] **Step 3: Unity 컴파일 확인**

Unity Editor에서 Console 창 열기 → 빨간 컴파일 에러 없음 확인.

- [ ] **Step 4: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/InteractRoleSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/InteractRoleSO.cs.meta"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/InteractPoint.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/InteractPoint.cs.meta"
git commit -m "feat: InteractRoleSO + InteractPoint struct 추가"
```

---

## Task 2: TileSetData 필드 제거

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/TileSetData.cs`

- [ ] **Step 1: TileSetData.cs를 아래로 교체**

```csharp
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [CreateAssetMenu(fileName = "ObstacleData", menuName = "GridSystem/Object")]
    public class TileSetData : ScriptableObject
    {
        public Vector2Int[] BlockedOffsets;
        public bool IsWalkable;
    }
}
```

> `Prefab`, `InteractOffsets`, `StaffInteractOffsets`, `IsInteractable`, `Id`, `OnValidate` 모두 제거.

- [ ] **Step 2: Unity 컴파일 확인**

컴파일 에러 목록을 확인한다. 이 시점에서 `ObstacleDataEditor`, `Workplace`, `ObjectDataRegistrySO` 등에서 에러가 발생하며, 이는 후속 태스크에서 순차적으로 해결한다. **현재 태스크에서 수정하는 파일은 TileSetData.cs 하나뿐.**

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Grid/Objects/TileSetData.cs"
git commit -m "refactor: TileSetData에서 Prefab/InteractOffsets/Id 제거"
```

---

## Task 3: ObjectDataSO — Id/WorkplacePrefab 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/ObjectDataSO.cs`

- [ ] **Step 1: ObjectDataSO.cs를 아래로 교체**

```csharp
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [CreateAssetMenu(fileName = "ObjectIconData", menuName = "GridSystem/ObjectIconData")]
    public class ObjectDataSO : ScriptableObject
    {
        [SerializeField] private string _id;
        public string Id => _id;

        [SerializeField] private Sprite _icon;
        public Sprite Icon => _icon;

        public string DisplayName;
        public string Description;
        public GameObject WorkplacePrefab;

#if UNITY_EDITOR
        private void OnValidate()
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (_id != guid)
            {
                _id = guid;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
```

> `TileSet` 필드 제거. `WorkplacePrefab` 추가. `Id`/`OnValidate` 추가.

- [ ] **Step 2: Unity 컴파일 확인 + OnValidate 트리거**

Unity에서 기존 `ObjectDataSO` 에셋을 모두 선택 → 인스펙터에서 값이 바뀌면 `_id`가 자동 부여된 것. (에셋이 아직 없으면 이 단계 생략 가능)

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Grid/Objects/ObjectDataSO.cs"
git commit -m "feat: ObjectDataSO에 Id(GUID)/WorkplacePrefab 추가, TileSet 제거"
```

---

## Task 4: ObjectDataRegistrySO — ObjectDataSO 기준으로 변경

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/ObjectDataRegistrySO.cs`

- [ ] **Step 1: ObjectDataRegistrySO.cs를 아래로 교체**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [CreateAssetMenu(fileName = "ObjectDataRegistry", menuName = "GridSystem/ObjectDataRegistry")]
    public class ObjectDataRegistrySO : ScriptableObject
    {
        [SerializeField] private List<ObjectDataSO> _objects = new();

        private Dictionary<string, ObjectDataSO> _dict;

        public void BuildRuntimeDict()
        {
            _dict = new Dictionary<string, ObjectDataSO>(_objects.Count);
            foreach (var obj in _objects)
            {
                if (obj != null && !string.IsNullOrEmpty(obj.Id))
                    _dict[obj.Id] = obj;
            }
        }

        public ObjectDataSO GetById(string id)
        {
            if (_dict == null) BuildRuntimeDict();
            _dict.TryGetValue(id, out var result);
            return result;
        }

#if UNITY_EDITOR
        [ContextMenu("Scan Project")]
        private void ScanProject()
        {
            _objects.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:ObjectDataSO");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var obj  = UnityEditor.AssetDatabase.LoadAssetAtPath<ObjectDataSO>(path);
                if (obj != null)
                    _objects.Add(obj);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ObjectDataRegistry] Scanned {_objects.Count} assets.");
        }
#endif
    }
}
```

- [ ] **Step 2: Unity 컴파일 확인**

ObjectDataRegistrySO에 연결된 에셋이 있으면, 인스펙터에서 `_objects` 리스트가 `ObjectDataSO` 타입으로 비어 있는 것을 확인. 에셋이 있다면 Scan Project 컨텍스트 메뉴로 재스캔.

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Grid/Objects/ObjectDataRegistrySO.cs"
git commit -m "refactor: ObjectDataRegistrySO를 ObjectDataSO 기준으로 인덱싱 변경"
```

---

## Task 5: PlacedObstacleEntry — ObjectDataSO + FlipX

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/PlacedObstacleEntry.cs`

- [ ] **Step 1: PlacedObstacleEntry.cs를 아래로 교체**

```csharp
using System;
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [Serializable]
    public struct PlacedObstacleEntry
    {
        public Vector2Int cellIndex;
        public ObjectDataSO obstacleData;
        public bool flipX;
    }
}
```

- [ ] **Step 2: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Grid/Objects/PlacedObstacleEntry.cs"
git commit -m "refactor: PlacedObstacleEntry — TileSetData→ObjectDataSO, flipX 추가"
```

---

## Task 6: GameSaveData — PlacedObstacleEntrySave에 FlipX 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Save/GameSaveData.cs`

- [ ] **Step 1: PlacedObstacleEntrySave에 FlipX 필드 추가**

`GameSaveData.cs`의 `PlacedObstacleEntrySave` 클래스를:

```csharp
[Serializable]
public class PlacedObstacleEntrySave
{
    public string     ObjectDataId;
    public Vector2Int CellIndex;
    public bool       FlipX;
}
```

- [ ] **Step 2: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Save/GameSaveData.cs"
git commit -m "feat: PlacedObstacleEntrySave에 FlipX 필드 추가"
```

---

## Task 7: GridManager — ApplyObstacleAt에 flipX 파라미터 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Grid/GridManager.cs`

- [ ] **Step 1: ApplyObstacleAt 메서드를 아래로 교체**

```csharp
public void ApplyObstacleAt(TileSetData data, Vector2Int cellIndex, bool flipX = false)
{
    if (data == null) return;

    if (TryGetCellToNode(cellIndex.x, cellIndex.y, out Node rootNode))
        rootNode.walkable = data.IsWalkable;

    if (data.BlockedOffsets == null) return;
    foreach (var offset in data.BlockedOffsets)
    {
        var applied = flipX ? new Vector2Int(-offset.x, offset.y) : offset;
        if (TryGetCellToNode(cellIndex.x + applied.x, cellIndex.y + applied.y, out Node offsetNode))
            offsetNode.walkable = data.IsWalkable;
    }
}
```

- [ ] **Step 2: 컴파일 확인 후 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Grid/GridManager.cs"
git commit -m "feat: GridManager.ApplyObstacleAt에 flipX 파라미터 추가"
```

---

## Task 8: Workplace — 전체 재설계

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Workplace/Workplace.cs`

- [ ] **Step 1: Workplace.cs를 아래로 교체**

```csharp
using BBJ.GridSystem;
using BBJ.GridSystem.Objects;
using BBJ.Register;
using System;
using System.Collections.Generic;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.GridSystem.Pathfind;

namespace BBJ.WorkplaceSystem
{
    public class Workplace : ModuleOwner
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO     _workplaceType;
        public WorkplaceTypeSO WorkplaceType => _workplaceType;

        [SerializeField] private TileSetData          _tileSetData;
        [SerializeField] private List<InteractPoint>  _interactPoints = new();
        [SerializeField] private bool                 _flipX;

        public TileSetData TileSetData => _tileSetData;

        // role → 유효한 월드 좌표 목록 (RefreshWorkPoints 이후 갱신)
        private readonly Dictionary<InteractRoleSO, List<Vector3>> _validPoints = new();

        protected override void Awake()
        {
            base.Awake();
            _register?.Register(this);
        }

        private void OnDestroy()
        {
            _register?.Unregister(this);
        }

        public void Setup(Vector2Int cellIndex, Func<Vector2Int, Vector3> cellToWorld, bool flipX)
        {
            _flipX = flipX;
            _validPoints.Clear();

            foreach (var ip in _interactPoints)
            {
                if (ip.Role == null) continue;

                var offset    = flipX ? new Vector2Int(-ip.Offset.x, ip.Offset.y) : ip.Offset;
                var worldPos  = cellToWorld(cellIndex + offset);

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

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            foreach (var ip in _interactPoints)
            {
                if (ip.Role == null) continue;
                Gizmos.color = ip.Role.GizmoColor;
                var offset   = _flipX ? new Vector2Int(-ip.Offset.x, ip.Offset.y) : ip.Offset;
                Gizmos.DrawSphere(transform.position + new Vector3(offset.x, 0f, offset.y) * 1f, 0.15f);
            }
        }
#endif
    }
}
```

> 이전의 `SetupFromObjectData`, `GetNearestStaffPoint`, `_workPoints`, `_staffWorkPoints`, `HasWorkPoints`, `StaffWorkPoints` 모두 제거.
>
> `OnDrawGizmos`에서 오프셋을 월드 좌표로 변환하는 방식은 프로젝트의 그리드 단위(1 unit = 1 cell)에 맞게 조정 필요. 실제 그리드 스케일이 다르면 에디터에서 GridManager의 CellToWorld를 참고하여 스케일을 조정한다.

- [ ] **Step 2: 컴파일 확인**

이 시점에서 `ObjectManager`가 `SetupFromObjectData` 참조 에러를 냄. Task 10에서 해결.

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Workplace.cs"
git commit -m "refactor: Workplace 재설계 — InteractPoint/InteractRoleSO 기반, Setup 콜백 방식"
```

---

## Task 9: SchedulingModule — InteractRoleSO 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs`

- [ ] **Step 1: using + 필드 추가**

기존 파일에서 `using BBJ.WorkplaceSystem;` 추가 후 클래스 필드에 아래를 삽입:

```csharp
[SerializeField] private InteractRoleSO _interactRole;
public InteractRoleSO InteractRole => _interactRole;
```

최종 파일:

```csharp
using BBJ.Order;
using BBJ.Register;
using BBJ.Staff;
using BBJ.Work;
using BBJ.WorkplaceSystem;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.EventSystem;

namespace BBJ.Schedule
{
    public class SchedulingModule : MonoBehaviour, IModule, ISchedulable, IAfterInitModule
    {
        [SerializeField] private ScheduleRegisterSO _scheduleRegister;
        [SerializeField] private EventChannelSO     _scheduleChannel;
        [SerializeField] private InteractRoleSO     _interactRole;
        [field: SerializeField] public AgentRole Role { get; private set; }

        public InteractRoleSO InteractRole => _interactRole;

        private ModuleOwner          _owner;
        private WorkExecutionContext _execCtx;

        public WorkSO      CurrentWork   { get; private set; }
        public OrderTicket CurrentTicket { get; private set; }

        public bool IsAvailableForWork => _execCtx == null;
        public bool IsWorkPaused => _execCtx?.IsPaused ?? false;

        public event Action OnWorkStarted;
        public event Action<bool> OnWorkEnded;

        public void Initialize(ModuleOwner owner)
        {
            this._owner = owner;
            UtilDebugger.AssertAllAssigned(this);
        }
        public void AfterInit()
        {
            _scheduleRegister.Register(this);
        }

        private void OnDisable()
        {
            _scheduleRegister.Unregister(this);
            CancelWork();
        }

        public void AssignWork(WorkSO workSO, OrderTicket ticket)
        {
            CancelWork();
            CurrentWork   = workSO;
            CurrentTicket = ticket;
            _execCtx      = new WorkExecutionContext();
            RunAsync(workSO, ticket, _execCtx).Forget();
        }

        public void CancelWork()
        {
            _execCtx?.HardCancel();
            _execCtx = null;
        }

        public void Pause()  => _execCtx?.Pause();
        public void Resume() => _execCtx?.Resume();

        private async UniTaskVoid RunAsync(
            WorkSO workSO, OrderTicket ticket, WorkExecutionContext ctx)
        {
            OnWorkStarted?.Invoke();
            WorkResult result = WorkResult.Cancelled;
            try
            {
                result = await workSO.ExecuteAsync(_owner, ticket, ctx);
            }
            catch (OperationCanceledException)
            {
                result = WorkResult.Cancelled;
            }
            finally
            {
                bool isCurrentCtx = _execCtx == ctx;
                if (isCurrentCtx)
                {
                    _execCtx      = null;
                    CurrentWork   = null;
                    CurrentTicket = null;
                }
                ctx.Dispose();
                OnWorkEnded?.Invoke(result == WorkResult.Completed);
                _scheduleChannel?.RaiseEvent(new ScheduleTriggerEvent());
            }
        }
    }
}
```

- [ ] **Step 2: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs"
git commit -m "feat: SchedulingModule에 InteractRoleSO _interactRole 추가"
```

---

## Task 10: ObjectManager — ObjectDataSO API로 전환

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/ObjectManager.cs`

- [ ] **Step 1: ObjectManager.cs 전체 교체**

```csharp
using BBJ.Save;
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;
using System.Collections.Generic;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Systems.SaveSystem;

namespace BBJ.GridSystem.Objects
{
    public class ObjectManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager           _gridManager;
        [SerializeField] private StageLayoutSO         _stageLayout;
        [SerializeField] private ObjectDataRegistrySO  _registry;

        [Header("Event Channels")]
        [SerializeField] private EventChannelSO _objectSpawnChannel;

        private const string SaveFile   = "stage.save";
        private const string SaveFolder = "BarTycoon";

        private readonly List<PlacedObstacleEntrySave> _placed = new();

        private void Awake()  { SubEventChannel(); }
        private void Start()  { LoadStageLayout(); }
        private void OnDestroy() { UnSubEventChannel(); }

        private void LoadStageLayout()
        {
            var workplaces = new List<Workplace>();

            if (SaveManager.IsSaveFile(SaveFile, SaveFolder))
            {
                var saveData = SaveManager.Load(typeof(StageSaveData), SaveFile, SaveFolder) as StageSaveData;
                if (saveData != null)
                {
                    _placed.Clear();
                    _registry?.BuildRuntimeDict();
                    foreach (var entry in saveData.PlacedObjects)
                    {
                        var data = _registry?.GetById(entry.ObjectDataId);
                        if (data == null) continue;
                        var wp = PlaceObjectInternal(data, entry.CellIndex, entry.FlipX);
                        if (wp != null) workplaces.Add(wp);
                        _placed.Add(entry);
                    }
                    foreach (var wp in workplaces)
                        wp.RefreshWorkPoints(_gridManager);
                    return;
                }
            }

            if (_stageLayout == null) return;
            foreach (var entry in _stageLayout.entries)
            {
                var wp = PlaceObjectInternal(entry.obstacleData, entry.cellIndex, entry.flipX);
                if (wp != null) workplaces.Add(wp);
                if (entry.obstacleData != null && !string.IsNullOrEmpty(entry.obstacleData.Id))
                    _placed.Add(new PlacedObstacleEntrySave
                        { ObjectDataId = entry.obstacleData.Id, CellIndex = entry.cellIndex, FlipX = entry.flipX });
            }
            foreach (var wp in workplaces)
                wp.RefreshWorkPoints(_gridManager);
            SaveStage();
        }

        public Workplace PlaceObject(ObjectDataSO data, Vector2Int cellIndex, bool flipX = false)
        {
            var wp = PlaceObjectInternal(data, cellIndex, flipX);
            if (data != null && !string.IsNullOrEmpty(data.Id))
            {
                _placed.Add(new PlacedObstacleEntrySave { ObjectDataId = data.Id, CellIndex = cellIndex, FlipX = flipX });
                SaveStage();
            }
            return wp;
        }

        private Workplace PlaceObjectInternal(ObjectDataSO data, Vector2Int cellIndex, bool flipX)
        {
            Vector3   worldPos  = _gridManager.CellToWorld(cellIndex);
            Workplace workplace = null;

            if (data?.WorkplacePrefab != null)
            {
                var go = Instantiate(data.WorkplacePrefab, worldPos, Quaternion.identity);
                workplace = go.GetComponent<Workplace>();
                workplace?.Setup(cellIndex, _gridManager.CellToWorld, flipX);

                if (workplace?.TileSetData != null)
                    _gridManager.ApplyObstacleAt(workplace.TileSetData, cellIndex, flipX);
            }

            workplace?.RefreshWorkPoints(_gridManager);
            return workplace;
        }

        private void SaveStage()
        {
            var data = new StageSaveData { PlacedObjects = new List<PlacedObstacleEntrySave>(_placed) };
            SaveManager.Save(data, SaveFile, SaveFolder);
        }

        private void SubEventChannel()
        {
            _objectSpawnChannel?.AddListener<ObjectSpawnEvent>(HandleSpawnObject);
        }
        private void UnSubEventChannel()
        {
            _objectSpawnChannel?.RemoveListener<ObjectSpawnEvent>(HandleSpawnObject);
        }
        private void HandleSpawnObject(ObjectSpawnEvent evt) => PlaceObject(evt.ObjectData, evt.CellIndex, evt.FlipX);
    }

    public class ObjectSpawnEvent : GameEvent
    {
        public ObjectDataSO ObjectData { get; private set; }
        public Vector2Int   CellIndex  { get; private set; }
        public bool         FlipX      { get; private set; }

        public ObjectSpawnEvent Init(ObjectDataSO data, Vector2Int cellIndex, bool flipX = false)
        {
            ObjectData = data;
            CellIndex  = cellIndex;
            FlipX      = flipX;
            return this;
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

이 시점에서 ObjectManager 관련 에러가 사라져야 한다.

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Grid/Objects/ObjectManager.cs"
git commit -m "refactor: ObjectManager — TileSetData→ObjectDataSO, FlipX 지원"
```

---

## Task 11: WorkSO 7종 — GetNearestPoint(role) 호출로 변경

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/ExitWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/TakeSeatWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/PayAtCounterWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs`

패턴: 각 WorkSO의 `RunAsync` 시작 부분에서 role을 꺼내고, `GetNearestPoint(from)` / `GetNearestStaffPoint(from)` 호출을 `GetNearestPoint(role, from)`으로 교체.

role 추출 헬퍼 (각 파일에 동일하게 적용):
```csharp
var role = executor.GetModule<BBJ.Schedule.SchedulingModule>()?.InteractRole;
```

- [ ] **Step 1: CashierWorkSO.cs — RunAsync 수정**

`GetNearestStaffPoint` → `GetNearestPoint(role, ...)` 으로 변경. `RunAsync` 전체:

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
{
    var actions = executor.GetModule<AgentActionModule>();
    if (actions == null || ticket == null) return WorkResult.Cancelled;

    if (!ticket.TryReserve(executor)) return WorkResult.Cancelled;

    var role    = executor.GetModule<BBJ.Schedule.SchedulingModule>()?.InteractRole;
    var counter = _ctx.WorkplaceRegister?.GetFirst(_ctx.CounterType);
    if (counter == null)
    {
        _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
        return WorkResult.Cancelled;
    }

    var queue = counter.GetModule<WorkplaceQueueModule>();
    if (queue == null)
    {
        _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
        return WorkResult.Cancelled;
    }

    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
        ctx.Token, ticket.Token);

    OccupationSlot? slot      = null;
    bool            processed = false;
    try
    {
        await actions.Execute<MoveAction>(
            a => a.ExecuteAsync(counter.GetNearestPoint(role, executor.transform.position), linked.Token));
        ticket.TryStartProgress(executor);
        var foodContext = executor.GetModule<FoodContextModule>();
        foodContext?.SetFood(ticket.Ordered);
        await actions.Execute<WaitAction>(
            a => a.ExecuteAsync(() => queue.HasWaiting, linked.Token));

        slot = queue.Dequeue();
        if (slot == null)
        {
            _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
            return WorkResult.Cancelled;
        }

        await actions.Execute<WorkAction>(
            a => a.ExecuteAsync(counter, linked.Token));

        slot.Value.NotifyProcessed();
        processed = true;

        _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, executor));
        return WorkResult.Completed;
    }
    catch (OperationCanceledException)
    {
        if (!ticket.IsTerminal)
            _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
        return WorkResult.Cancelled;
    }
    finally
    {
        if (!processed && slot.HasValue)
            slot.Value.NotifyProcessed();
    }
}
```

- [ ] **Step 2: CookWorkSO.cs — RunAsync 수정**

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
{
    var actions = executor.GetModule<AgentActionModule>();
    if (actions == null || ticket == null) return WorkResult.Cancelled;

    if (!ticket.TryReserve(executor)) return WorkResult.Cancelled;

    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
        ctx.Token, ticket.Token);

    var role    = executor.GetModule<BBJ.Schedule.SchedulingModule>()?.InteractRole;
    var kitchen = _ctx.WorkplaceRegister
        .GetCandidates(executor.transform.position, _ctx.KitchenType)
        .FirstOrDefault(k => k.GetModule<OccupancyModule>()?.TryReserve(executor, null) == true);

    if (kitchen == null)
    {
        _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
        return WorkResult.Cancelled;
    }

    var foodContext = executor.GetModule<FoodContextModule>();
    try
    {
        await actions.Execute<MoveAction>(
            a => a.ExecuteAsync(kitchen.GetNearestPoint(role, executor.transform.position), linked.Token));
        ticket.TryStartProgress(executor);
        foodContext?.SetFood(ticket.Ordered);

        _ctx.OrderChannel?.RaiseEvent(new CookingStartEvent(ticket, executor));
        await actions.Execute<WorkAction>(a => a.ExecuteAsync(kitchen, linked.Token));

        _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, executor));
        return WorkResult.Completed;
    }
    catch (OperationCanceledException)
    {
        if (!ticket.IsTerminal)
            _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
        return WorkResult.Cancelled;
    }
    finally
    {
        foodContext?.ClearFood();
        kitchen.GetModule<OccupancyModule>()?.Release();
    }
}
```

- [ ] **Step 3: ExitWorkSO.cs — RunAsync 수정**

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
{
    var customer = executor as CustomerAgent;
    if (customer == null) return WorkResult.Cancelled;

    var seat    = customer.AssignedSeat;
    var actions = executor.GetModule<AgentActionModule>();

    if (seat != null)
    {
        seat.GetModule<SeatModule>()?.ClearCustomer();
        seat.GetModule<OccupancyModule>()?.Release();
        customer.AssignedSeat = null;

        var exits = _ctx.WorkplaceRegister?.GetAll(_ctx.ExitType);
        if (exits != null && exits.Count > 0 && actions != null)
        {
            var role = executor.GetModule<BBJ.Schedule.SchedulingModule>()?.InteractRole;
            await actions.Execute<MoveAction>(
                a => a.ExecuteAsync(exits[0].GetNearestPoint(role, executor.transform.position), ctx.Token));
        }
    }

    _ctx.CustomerChannel?.RaiseEvent(new CustomerLeftEvent { Customer = customer });
    return WorkResult.Completed;
}
```

- [ ] **Step 4: TakeSeatWorkSO.cs — RunAsync 수정**

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
{
    var customer = executor as CustomerAgent;
    var actions  = executor.GetModule<AgentActionModule>();
    if (customer == null || actions == null) return WorkResult.Cancelled;

    var seat = _ctx.WorkplaceRegister
        .GetCandidates(executor.transform.position, _ctx.SeatType)
        .FirstOrDefault(s => {
            var occ = s.GetModule<OccupancyModule>();
            return occ != null && !occ.IsOccupied && occ.TryReserve(executor, null);
        });

    if (seat == null) return WorkResult.Cancelled;

    customer.AssignedSeat = seat;
    seat.GetModule<OccupancyModule>()?.Occupy(executor);

    var role       = executor.GetModule<BBJ.Schedule.SchedulingModule>()?.InteractRole;
    var dest       = seat.GetNearestPoint(role, executor.transform.position);
    var seatModule = seat.GetModule<SeatModule>();
    seatModule?.AssignCustomer(executor);

    bool seated = false;
    try
    {
        await actions.Execute<MoveAction>(a => a.ExecuteAsync(dest, ctx.Token));
        seatModule?.Seat(executor);
        seated = true;
        return WorkResult.Completed;
    }
    finally
    {
        if (!seated)
        {
            seat.GetModule<OccupancyModule>()?.Release();
            customer.AssignedSeat = null;
        }
    }
}
```

- [ ] **Step 5: TakeOrderWorkSO.cs — RunAsync 수정**

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
{
    var actions  = executor.GetModule<AgentActionModule>();
    var customer = ticket?.Customer as CustomerAgent;
    if (actions == null || ticket == null || customer == null) return WorkResult.Cancelled;

    if (ticket.WorkPhase != OrderWorkPhase.ReadyForServer) return WorkResult.Cancelled;
    if (!ticket.TryReserve(executor)) return WorkResult.Cancelled;

    using var linked = CancellationTokenSource.CreateLinkedTokenSource(ctx.Token, ticket.Token);
    customer.SetAssignedServer(executor);
    var role = executor.GetModule<BBJ.Schedule.SchedulingModule>()?.InteractRole;
    try
    {
        await actions.Execute<MoveAction>(
            a => a.ExecuteAsync(ticket.Seat.GetNearestPoint(role, executor.transform.position), linked.Token));
        ticket.TryStartProgress(executor);

        await actions.Execute<WorkAction>(a => a.ExecuteAsync(ticket.Seat, linked.Token));

        _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, executor));
        return WorkResult.Completed;
    }
    catch (OperationCanceledException)
    {
        if (!ticket.IsTerminal)
            _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
        return WorkResult.Cancelled;
    }
    finally
    {
        customer.SetAssignedServer(null);
    }
}
```

- [ ] **Step 6: PayAtCounterWorkSO.cs — RunAsync 수정**

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
{
    var customer = executor as CustomerAgent;
    var actions  = executor.GetModule<AgentActionModule>();
    if (customer == null || actions == null) return WorkResult.Cancelled;

    customer.AssignedSeat?.GetModule<SeatModule>()?.UnSeat();
    var counter = _ctx.WorkplaceRegister?.GetFirst(_ctx.CounterType);
    if (counter == null) return WorkResult.Cancelled;

    var role = executor.GetModule<BBJ.Schedule.SchedulingModule>()?.InteractRole;
    await actions.Execute<MoveAction>(
        a => a.ExecuteAsync(counter.GetNearestPoint(role, executor.transform.position), ctx.Token));

    var payQueue = counter.GetModule<WorkplaceQueueModule>();
    if (payQueue == null) return WorkResult.Cancelled;

    bool paid = false;
    var slot = new OccupationSlot(
        executor.transform,
        pos => actions.Execute<MoveAction>(a => a.ExecuteAsync(pos, ctx.Token)).Forget(),
        () => { customer.OnPaymentDone(); paid = true; });
    payQueue.Enqueue(slot);

    await actions.Execute<WaitAction>(a => a.ExecuteAsync(() => paid, ctx.Token));
    return WorkResult.Completed;
}
```

- [ ] **Step 7: ServeWorkSO.cs — RunAsync 수정 (GetNearestPoint ×2)**

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, OrderTicket ticket, WorkExecutionContext ctx)
{
    var actions = executor.GetModule<AgentActionModule>();
    if (actions == null || ticket == null) return WorkResult.Cancelled;

    if (!ticket.TryReserve(executor)) return WorkResult.Cancelled;

    var serveStation = _ctx.WorkplaceRegister?.GetFirst(_ctx.ServeStationType);
    if (serveStation == null)
    {
        _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
        return WorkResult.Cancelled;
    }

    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
        ctx.Token, ticket.Token);

    var role = executor.GetModule<BBJ.Schedule.SchedulingModule>()?.InteractRole;
    try
    {
        Vector3 from = executor.transform.position;
        await actions.Execute<MoveAction>(
            a => a.ExecuteAsync(serveStation.GetNearestPoint(role, from), linked.Token));
        ticket.TryStartProgress(executor);

        await actions.Execute<MoveAction>(
            a => a.ExecuteAsync(ticket.Seat.GetNearestPoint(role, from), linked.Token));
        await actions.Execute<WorkAction>(
            a => a.ExecuteAsync(ticket.Seat, linked.Token));

        var customer = ticket.Seat.GetModule<SeatModule>()?.AssignedAgent as CustomerAgent;
        customer?.OnFoodServed();

        _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ticket, executor));
        return WorkResult.Completed;
    }
    catch (OperationCanceledException)
    {
        if (!ticket.IsTerminal)
            _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ticket, executor));
        return WorkResult.Cancelled;
    }
}
```

- [ ] **Step 8: 컴파일 확인**

Console 에러 없음 확인. `GetNearestStaffPoint`, `GetNearestPoint(Vector3)` 참조 에러가 모두 사라져야 한다.

- [ ] **Step 9: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/"
git commit -m "refactor: WorkSO 7종 — GetNearestPoint(role, from) 호출로 전환"
```

---

## Task 12: WorkplaceEditor — Gizmo + 인스펙터 Custom Editor

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceEditor.cs`

- [ ] **Step 1: Editor 폴더 생성 확인**

`Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/` 폴더가 없으면 Unity에서 생성.

- [ ] **Step 2: WorkplaceEditor.cs 작성**

```csharp
#if UNITY_EDITOR
using BBJ.GridSystem;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Editor
{
    [CustomEditor(typeof(Workplace))]
    public class WorkplaceEditor : UnityEditor.Editor
    {
        private SerializedProperty _tileSetDataProp;
        private SerializedProperty _interactPointsProp;
        private SerializedProperty _flipXProp;

        private void OnEnable()
        {
            _tileSetDataProp    = serializedObject.FindProperty("_tileSetData");
            _interactPointsProp = serializedObject.FindProperty("_interactPoints");
            _flipXProp          = serializedObject.FindProperty("_flipX");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Workplace 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_register"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_workplaceType"));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("타일 & 인터랙션", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_tileSetDataProp);
            EditorGUILayout.PropertyField(_flipXProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_interactPointsProp, new GUIContent("InteractPoints"), true);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            var wp = (Workplace)target;
            var points = serializedObject.FindProperty("_interactPoints");
            bool flipX = serializedObject.FindProperty("_flipX").boolValue;

            for (int i = 0; i < points.arraySize; i++)
            {
                var element = points.GetArrayElementAtIndex(i);
                var offsetProp = element.FindPropertyRelative("Offset");
                var roleProp   = element.FindPropertyRelative("Role");

                var roleObj = roleProp.objectReferenceValue as InteractRoleSO;
                Color gizmoColor = roleObj != null ? roleObj.GizmoColor : Color.white;

                Vector2Int offset = new Vector2Int(offsetProp.FindPropertyRelative("x").intValue,
                                                   offsetProp.FindPropertyRelative("y").intValue);
                if (flipX) offset = new Vector2Int(-offset.x, offset.y);

                // 그리드 단위 → 월드 좌표 (GridManager 없이 근사치)
                Vector3 worldPos = wp.transform.position + new Vector3(offset.x, 0f, offset.y);

                Handles.color = gizmoColor;
                Handles.SphereHandleCap(0, worldPos, Quaternion.identity, 0.2f, EventType.Repaint);

                Handles.Label(worldPos + Vector3.up * 0.3f,
                    $"[{i}] {(roleObj != null ? roleObj.name : "No Role")} ({offset.x},{offset.y})",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = gizmoColor } });
            }
        }
    }
}
#endif
```

> `OnSceneGUI`에서 오프셋→월드 좌표 변환은 GridManager 의존 없이 근사값(`transform.position + offset`)을 사용한다. 실제 그리드 스케일이 1 unit/cell이 아니라면 GridManager를 `FindObjectOfType`으로 찾아 `CellToWorld`를 쓰도록 수정 가능.

- [ ] **Step 3: Unity에서 Workplace 컴포넌트가 있는 GameObject 선택 후 인스펙터/씬 뷰 확인**

인스펙터에 `TileSetData`, `FlipX`, `InteractPoints` 필드가 보이는지 확인.  
씬 뷰에서 InteractPoint 구체(sphere)가 역할 색상으로 표시되는지 확인.

- [ ] **Step 4: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceEditor.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/WorkplaceEditor.cs.meta"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Editor/"
git commit -m "feat: WorkplaceEditor — InteractPoint Gizmo + 인스펙터 Custom Editor"
```

---

## Task 13: ObstacleDataEditor — InteractOffset UI 제거

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Grid/Objects/Editor/ObstacleDataEditor.cs`

- [ ] **Step 1: 파일 전체 교체 — Blocked 전용으로 축소**

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BBJ.GridSystem.Objects.Editor
{
    [CustomEditor(typeof(TileSetData))]
    public class ObstacleDataEditor : UnityEditor.Editor
    {
        private const float CanvasHeight = 320f;
        private const float TileSizeBase = 48f;
        private const float ZoomMin = 0.4f;
        private const float ZoomMax = 2.0f;
        private const float ZoomStep = 0.1f;
        private const int RangeMin = 1;
        private const int RangeMax = 8;

        private static readonly Color ColEmpty        = new Color(0.22f, 0.24f, 0.28f, 1.00f);
        private static readonly Color ColBorderEmpty  = new Color(0.35f, 0.38f, 0.44f, 1.00f);
        private static readonly Color ColOrigin       = new Color(0.98f, 0.78f, 0.46f, 0.95f);
        private static readonly Color ColOriginBorder = new Color(0.93f, 0.62f, 0.09f, 1.00f);
        private static readonly Color ColOriginText   = new Color(0.30f, 0.15f, 0.01f, 1.00f);
        private static readonly Color ColBlocked      = new Color(0.80f, 0.22f, 0.22f, 0.90f);
        private static readonly Color ColBlockedBorder= new Color(1.00f, 0.40f, 0.40f, 1.00f);
        private static readonly Color ColHoverBlock   = new Color(1.00f, 0.45f, 0.45f, 0.35f);
        private static readonly Color ColText         = new Color(1.00f, 1.00f, 1.00f, 0.80f);
        private static readonly Color ColPanHint      = new Color(1.00f, 1.00f, 1.00f, 0.30f);

        private int _viewMinX = -3, _viewMaxX = 3;
        private int _viewMinY = -3, _viewMaxY = 3;
        private Vector2 _panOffset = Vector2.zero;
        private float _zoom = 1.0f;

        private bool    _isPanning;
        private Vector2 _panStartMouse;
        private Vector2 _panStartOffset;

        private bool      _isDragPainting;
        private bool      _dragErasing;
        private Vector2Int _lastDragCell = new Vector2Int(int.MinValue, int.MinValue);

        private HashSet<Vector2Int> _blocked = new HashSet<Vector2Int>();
        private Vector2Int _hovered = new Vector2Int(int.MinValue, int.MinValue);

        private float TileW => TileSizeBase * _zoom;
        private float TileH => TileSizeBase * 0.5f * _zoom;
        private int ViewCols => _viewMaxX - _viewMinX + 1;
        private int ViewRows => _viewMaxY - _viewMinY + 1;

        private void OnEnable() { LoadFromSO(); }

        private void LoadFromSO()
        {
            var so = (TileSetData)target;
            _blocked.Clear();
            if (so.BlockedOffsets != null)
                foreach (var v in so.BlockedOffsets) _blocked.Add(v);
        }

        private void SaveToSO()
        {
            var so = (TileSetData)target;
            Undo.RecordObject(so, "Edit Obstacle Tile");
            so.BlockedOffsets = _blocked.ToArray();
            EditorUtility.SetDirty(so);
        }

        private Vector2 GridToScreen(int gx, int gy, Rect canvas)
        {
            float midGx = (_viewMinX + _viewMaxX) * 0.5f;
            float midGy = (_viewMinY + _viewMaxY) * 0.5f;
            float pivotX = canvas.x + canvas.width * 0.5f - (midGx - midGy) * (TileW * 0.5f) + _panOffset.x;
            float pivotY = canvas.y + canvas.height * 0.5f + (midGx + midGy) * (TileH * 0.5f) + _panOffset.y;
            return new Vector2(pivotX + (gx - gy) * (TileW * 0.5f), pivotY - (gx + gy) * (TileH * 0.5f));
        }

        private bool ScreenToGrid(Vector2 mouse, Rect canvas, out Vector2Int cell)
        {
            float bestDist = float.MaxValue;
            cell = default;
            bool found = false;
            for (int gy = _viewMinY; gy <= _viewMaxY; gy++)
                for (int gx = _viewMinX; gx <= _viewMaxX; gx++)
                {
                    Vector2 center = GridToScreen(gx, gy, canvas);
                    float dx = Mathf.Abs(mouse.x - center.x) / (TileW * 0.5f);
                    float dy = Mathf.Abs(mouse.y - center.y) / (TileH * 0.5f);
                    if (dx + dy <= 1.0f)
                    {
                        float dist = (mouse - center).sqrMagnitude;
                        if (dist < bestDist) { bestDist = dist; cell = new Vector2Int(gx, gy); found = true; }
                    }
                }
            return found;
        }

        private bool PaintTile(Vector2Int coord, bool erasing)
        {
            if (coord == Vector2Int.zero) return false;
            if (erasing)
            {
                if (!_blocked.Contains(coord)) return false;
                _blocked.Remove(coord);
                return true;
            }
            else
            {
                if (_blocked.Contains(coord)) return false;
                _blocked.Add(coord);
                return true;
            }
        }

        private void DrawDiamond(Vector2 c, Color fill, Color border)
        {
            float hw = TileW * 0.5f, hh = TileH * 0.5f;
            Handles.DrawSolidRectangleWithOutline(new Vector3[]
            {
                new Vector3(c.x,      c.y - hh),
                new Vector3(c.x + hw, c.y      ),
                new Vector3(c.x,      c.y + hh),
                new Vector3(c.x - hw, c.y      ),
            }, fill, border);
        }

        private void DrawLabel(Vector2 c, string text, Color color)
        {
            int size = Mathf.RoundToInt(Mathf.Clamp(9f * _zoom, 7f, 12f));
            GUI.Label(
                new Rect(c.x - TileW * 0.5f, c.y - TileH * 0.5f, TileW, TileH),
                text,
                new GUIStyle(EditorStyles.label)
                {
                    fontSize = size,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = color }
                });
        }

        private void DrawRangeRow(string axisLabel, ref int refMin, ref int refMax)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(axisLabel, EditorStyles.miniBoldLabel, GUILayout.Width(14));
                GUILayout.Label("음:", EditorStyles.miniLabel, GUILayout.Width(24));
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(17)) && refMin > -RangeMax) { refMin--; Repaint(); }
                GUILayout.Label(refMin.ToString(), EditorStyles.miniLabel, GUILayout.Width(20));
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(17)) && refMin < -RangeMin) { refMin++; Repaint(); }
                GUILayout.Space(10);
                GUILayout.Label("양:", EditorStyles.miniLabel, GUILayout.Width(24));
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(17)) && refMax > RangeMin) { refMax--; Repaint(); }
                GUILayout.Label(refMax.ToString(), EditorStyles.miniLabel, GUILayout.Width(20));
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(17)) && refMax < RangeMax) { refMax++; Repaint(); }
                GUILayout.FlexibleSpace();
                GUILayout.Label($"범위 {refMin} ~ {refMax}", EditorStyles.miniLabel, GUILayout.Width(72));
            }
        }

        private List<Vector2Int> OutOfView(HashSet<Vector2Int> set) =>
            set.Where(v => v.x < _viewMinX || v.x > _viewMaxX ||
                           v.y < _viewMinY || v.y > _viewMaxY).ToList();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IsWalkable"));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("점유 오프셋 에디터 (쿼터뷰)", EditorStyles.boldLabel);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("그리드 뷰 범위  (에디터 전용)", EditorStyles.miniBoldLabel);
            DrawRangeRow("X", ref _viewMinX, ref _viewMaxX);
            DrawRangeRow("Y", ref _viewMinY, ref _viewMaxY);

            var outBlocked = OutOfView(_blocked);
            if (outBlocked.Count > 0)
            {
                string msg = $"점유 밖: {string.Join(", ", outBlocked.Select(v => $"({v.x},{v.y})"))}";
                EditorGUILayout.HelpBox(msg + "\n데이터는 SO에 유지됩니다.", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("줌", EditorStyles.miniBoldLabel, GUILayout.Width(24));
                if (GUILayout.Button("-", GUILayout.Width(22), GUILayout.Height(17)))
                { _zoom = Mathf.Max(ZoomMin, _zoom - ZoomStep); Repaint(); }
                EditorGUILayout.LabelField($"{_zoom * 100f:F0}%", EditorStyles.miniLabel, GUILayout.Width(36));
                if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(17)))
                { _zoom = Mathf.Min(ZoomMax, _zoom + ZoomStep); Repaint(); }
                GUILayout.Space(8);
                if (GUILayout.Button("뷰 초기화", GUILayout.Width(70), GUILayout.Height(17)))
                { _zoom = 1f; _panOffset = Vector2.zero; Repaint(); }
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(6);

            Rect canvasRect = GUILayoutUtility.GetRect(0, CanvasHeight);
            canvasRect.x = 0;
            canvasRect.width = EditorGUIUtility.currentViewWidth - 4f;
            EditorGUI.DrawRect(canvasRect, new Color(0.13f, 0.13f, 0.16f, 1f));
            Handles.DrawSolidRectangleWithOutline(
                new[] {
                    new Vector3(canvasRect.xMin, canvasRect.yMin),
                    new Vector3(canvasRect.xMax, canvasRect.yMin),
                    new Vector3(canvasRect.xMax, canvasRect.yMax),
                    new Vector3(canvasRect.xMin, canvasRect.yMax),
                }, Color.clear, new Color(1f, 1f, 1f, 0.12f));

            Event e = Event.current;
            bool inCanvas = canvasRect.Contains(e.mousePosition);
            Rect localCanvas = new Rect(0, 0, canvasRect.width, canvasRect.height);

            if (inCanvas && e.type == EventType.ScrollWheel)
            {
                float prev = _zoom;
                _zoom = Mathf.Clamp(_zoom - e.delta.y * ZoomStep * 0.5f, ZoomMin, ZoomMax);
                Vector2 ml = e.mousePosition - new Vector2(canvasRect.x + canvasRect.width * 0.5f,
                                                           canvasRect.y + canvasRect.height * 0.5f);
                _panOffset = ml + (_panOffset - ml) * (_zoom / prev);
                e.Use(); Repaint();
            }

            if (inCanvas && e.type == EventType.MouseDown && e.button == 1)
            { _isPanning = true; _panStartMouse = e.mousePosition; _panStartOffset = _panOffset; e.Use(); }
            if (_isPanning)
            {
                if (e.type == EventType.MouseDrag && e.button == 1)
                { _panOffset = _panStartOffset + (e.mousePosition - _panStartMouse); e.Use(); Repaint(); }
                if (e.type == EventType.MouseUp && e.button == 1)
                { _isPanning = false; e.Use(); }
            }

            if (inCanvas && e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                if (ScreenToGrid(localMouse, localCanvas, out Vector2Int hv) && hv != Vector2Int.zero)
                {
                    _dragErasing = _blocked.Contains(hv);
                    if (PaintTile(hv, _dragErasing)) SaveToSO();
                    _isDragPainting = true;
                    _lastDragCell = hv;
                }
                e.Use();
            }

            if (_isDragPainting)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                    if (ScreenToGrid(localMouse, localCanvas, out Vector2Int hv) && hv != _lastDragCell)
                    {
                        if (PaintTile(hv, _dragErasing)) SaveToSO();
                        _lastDragCell = hv;
                    }
                    Repaint(); e.Use();
                }
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    _isDragPainting = false;
                    _lastDragCell = new Vector2Int(int.MinValue, int.MinValue);
                    e.Use();
                }
            }

            if (!_isPanning && (e.type == EventType.MouseMove || (e.type == EventType.MouseDrag && e.button == 0)))
            {
                if (inCanvas)
                {
                    Vector2 localMouse = e.mousePosition - new Vector2(canvasRect.x, canvasRect.y);
                    if (!ScreenToGrid(localMouse, localCanvas, out _hovered))
                        _hovered = new Vector2Int(int.MinValue, int.MinValue);
                }
                else _hovered = new Vector2Int(int.MinValue, int.MinValue);
                Repaint();
            }

            if (e.type == EventType.Repaint)
            {
                GUI.BeginClip(canvasRect);

                for (int gy = _viewMinY; gy <= _viewMaxY; gy++)
                    for (int gx = _viewMinX; gx <= _viewMaxX; gx++)
                    {
                        var coord  = new Vector2Int(gx, gy);
                        bool isOrig = coord == Vector2Int.zero;
                        bool isBlk  = _blocked.Contains(coord);
                        bool isHov  = coord == _hovered;
                        Vector2 ctr = GridToScreen(gx, gy, localCanvas);

                        if (ctr.x < -TileW || ctr.x > localCanvas.width + TileW ||
                            ctr.y < -TileH || ctr.y > localCanvas.height + TileH) continue;

                        Color fill, border;
                        if (isOrig)     { fill = ColOrigin;  border = ColOriginBorder; }
                        else if (isBlk) { fill = ColBlocked; border = ColBlockedBorder; }
                        else            { fill = ColEmpty;   border = ColBorderEmpty; }

                        DrawDiamond(ctr, fill, border);

                        if (isHov && !isOrig)
                            DrawDiamond(ctr, ColHoverBlock, Color.clear);

                        if (TileW >= 28f)
                        {
                            string lbl = isOrig ? "0,0" : $"{gx},{gy}";
                            Color tc = isOrig ? ColOriginText : ColText;
                            DrawLabel(ctr, lbl, tc);
                        }
                    }

                {
                    string tag = _isDragPainting
                        ? (_dragErasing ? "지우는 중 (점유)" : "그리는 중 (점유)")
                        : "브러시: 점유";
                    GUI.Label(new Rect(8, 6, 220, 18), tag,
                        new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.6f, 0.6f, 0.9f) } });
                }

                if (_isPanning)
                    GUI.Label(new Rect(0, 4, localCanvas.width - 6, 20), "패닝 중...",
                        new GUIStyle(EditorStyles.miniLabel)
                        { alignment = TextAnchor.UpperRight, normal = { textColor = ColPanHint } });

                DrawAxisLegend(localCanvas);
                GUI.EndClip();
            }

            EditorGUILayout.Space(2);
            GUILayout.Label(
                "우클릭 드래그: 패닝   |   스크롤: 줌   |   좌클릭/드래그: 타일 페인팅",
                new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 1f, 1f, 0.3f) } });

            EditorGUILayout.Space(6);

            GUI.color = new Color(1f, 0.6f, 0.6f, 1f);
            string blkText = _blocked.Count == 0 ? "BlockedOffsets: (없음)"
                : "BlockedOffsets: " + string.Join("  ", _blocked.OrderBy(v => v.y).ThenBy(v => v.x).Select(v => $"({v.x},{v.y})"));
            EditorGUILayout.LabelField(blkText, EditorStyles.miniLabel);
            GUI.color = Color.white;

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.color = new Color(1f, 0.6f, 0.6f, 1f);
                if (GUILayout.Button("점유 초기화", GUILayout.Width(80)))
                    if (EditorUtility.DisplayDialog("초기화", "모든 점유 타일을 삭제하시겠습니까?", "삭제", "취소"))
                    { _blocked.Clear(); SaveToSO(); }

                GUI.color = Color.white;
                if (GUILayout.Button("뷰 리셋", GUILayout.Width(60)))
                { _viewMinX = _viewMinY = -3; _viewMaxX = _viewMaxY = 3; _zoom = 1f; _panOffset = Vector2.zero; Repaint(); }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawAxisLegend(Rect canvas)
        {
            float ox = canvas.width - 80f;
            float oy = canvas.height - 52f;
            float len = 36f;
            Vector2 xEnd = new Vector2(ox + len * 0.707f, oy + len * 0.354f);
            Handles.color = new Color(1f, 0.40f, 0.40f, 0.85f);
            Handles.DrawLine(new Vector3(ox, oy), new Vector3(xEnd.x, xEnd.y));
            GUI.Label(new Rect(xEnd.x + 2, xEnd.y - 8, 24, 16), "-X",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.5f, 0.5f, 0.85f) } });
            Vector2 yEnd = new Vector2(ox - len * 0.707f, oy + len * 0.354f);
            Handles.color = new Color(0.40f, 1f, 0.75f, 0.85f);
            Handles.DrawLine(new Vector3(ox, oy), new Vector3(yEnd.x, yEnd.y));
            GUI.Label(new Rect(yEnd.x - 26, yEnd.y - 8, 24, 16), "-Y",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 1f, 0.85f, 0.85f) } });
        }
    }
}
```

- [ ] **Step 3: Unity에서 TileSetData SO 선택 후 인스펙터 확인**

브러시가 "점유" 하나만 남아 있고, InteractOffset/StaffInteractOffset UI가 없는 것을 확인.

- [ ] **Step 4: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Grid/Objects/Editor/ObstacleDataEditor.cs"
git commit -m "refactor: ObstacleDataEditor — Blocked 전용으로 축소, InteractOffset UI 제거"
```

---

## 전체 완료 후 에디터 작업 체크리스트

- [ ] 기존 `TileSetData` SO 에셋: `Prefab` 필드가 사라진 것 확인 (에러 없음)
- [ ] 기존 `ObjectDataSO` 에셋: `WorkplacePrefab` 필드 새로 연결, `_id` GUID 자동 부여 확인
- [ ] `ObjectDataRegistrySO` 에셋: Scan Project → `ObjectDataSO` 목록 재생성
- [ ] Workplace 프리팹: `_tileSetData`, `_interactPoints`, `_flipX` 설정
- [ ] `SchedulingModule`이 붙은 Agent 프리팹: `_interactRole` SO 연결 (손님/스태프별)
- [ ] `stage.save` 파일 삭제 (기존 저장 파일 무효화)
- [ ] Play Mode 실행 후 Console 경고/에러 없음 확인
- [ ] 손님이 좌석에 정상 착석하는 흐름 확인 (TakeSeatWork → GetNearestPoint)
- [ ] 스태프가 카운터로 정상 이동하는 흐름 확인 (CashierWork → GetNearestPoint)
