# Save System 설계 문서

> 작성일: 2026-05-21  
> 브랜치: BBJ  
> 관련 플랜: OrderTicket·Staff·StageLayout 저장 및 복원

---

## 1. 전체 구조

### GameSaveData (중첩 서브 클래스)

```csharp
[Serializable]
public class GameSaveData
{
    public StageSaveData    Stage;
    public OrdersSaveData   Orders;
    public StaffSaveData    Staff;
}

[Serializable]
public class StageSaveData
{
    public List<PlacedObstacleEntrySave> PlacedObjects;
}

[Serializable]
public class PlacedObstacleEntrySave
{
    public string       ObjectDataId;   // ObjectData GUID
    public Vector2Int   CellIndex;
}

[Serializable]
public class OrdersSaveData
{
    public List<OrderTicketSaveData> Tickets;
}

[Serializable]
public class OrderTicketSaveData
{
    public string           RecipeId;   // CocktailRecipeSO GUID
    public OrderWorkPhase   WorkPhase;
}

[Serializable]
public class StaffSaveData
{
    public List<StaffMemberSaveData> Members;
}

[Serializable]
public class StaffMemberSaveData
{
    public AgentRole    Role;
    public Vector3      LastPosition;
}
```

### 저장 타이밍

| 데이터 | 저장 시점 |
|--------|-----------|
| StageLayout | 오브젝트 배치·제거 즉시 |
| OrderTickets | 씬 전환 또는 게임 종료 시 |
| Staff | 씬 전환 또는 게임 종료 시 |

---

## 2. ObjectData 변경사항

### 추가 필드

```csharp
public class ObjectData : ScriptableObject
{
    // 기존
    public GameObject   Prefab;
    public Vector2Int[] BlockedOffsets;
    public Vector2Int[] InteractOffsets;
    public bool         IsWalkable;

    // 신규
    [SerializeField] string _id;                        // GUID 자동 베이크
    public Vector2Int[]     StaffInteractOffsets;       // Staff 전용 위치 (Counter 등)
    public ObjectIconDataSO IconData;                   // 도감 UI용

    public string Id => _id;

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
```

### ObjectIconDataSO (신규)

```csharp
public class ObjectIconDataSO : ScriptableObject
{
    [SerializeField] private Sprite _icon;
    public string DisplayName;
    public string Description;

    public Sprite Icon => _icon;
}
```

### ObjectDataRegistrySO (신규)

- `List<ObjectData>` 보유
- Inspector에 **"Scan Project"** 버튼 → `AssetDatabase.FindAssets`로 전체 스캔 자동 등록
- 런타임에 `Dictionary<string, ObjectData>` 빌드해서 GUID → ObjectData 조회

---

## 3. ObjectManager 변경사항

```
LoadStageLayout() 수정:
  if SaveManager.IsSaveFile("stage")
      → StageSaveData 로드
      → ObjectDataRegistrySO로 GUID → ObjectData 복원
      → 각 entry 배치
  else
      → 기존 StageLayoutSO에서 로드
```

- `ObjectSpawnEvent`는 수정하지 않음 (런타임 단일 배치용 유지)
- 오브젝트 배치·제거 시 `StageSaveData` 즉시 저장

---

## 4. ObstacleDataEditor 변경사항

`BrushMode` 열거형에 `StaffInteract` 추가:

```csharp
enum BrushMode { Blocked, Interact, StaffInteract }
```

- 기존 2모드 브러시에 StaffInteract 모드 추가
- Counter ObjectData에서 Staff가 서는 위치(카운터 뒤)를 시각적으로 편집 가능

---

## 5. Workplace 변경사항

`Workplace.SetupFromObjectData()` 수정:

```csharp
// 기존: _validWorkPoints (InteractOffsets 기반)
// 신규: _staffWorkPoints (StaffInteractOffsets 기반) 별도 리스트 추가
public List<Vector3> StaffWorkPoints { get; private set; }
```

`CashierWorkSO.RunAsync()`에서 `workplace.StaffWorkPoints`를 참조해 이동.

---

## 6. OrderTicket 변경사항

### OrderTicketSaveData 필드
- `string RecipeId` — CocktailRecipeSO GUID
- `OrderWorkPhase WorkPhase` — 현재 단계
- Seat·Customer는 저장하지 않음

### _readyCount 복원
별도 저장 없음. 로드 후 `WorkPhase == ReadyForServe`인 티켓 수를 집계해서 `PlayerOrderHandle._readyCount` 재구성.

---

## 7. StaffManager (신규)

### 구조

```csharp
public class StaffManager : MonoBehaviour
{
    [Serializable]
    public struct StaffEntry
    {
        public StaffConfigSO Config;
        public Transform     SpawnPoint;
    }

    [SerializeField] private List<StaffEntry> _entries;
}
```

### StaffConfigSO (신규)

```csharp
public class StaffConfigSO : ScriptableObject
{
    public AgentRole     Role;
    public GameObject    Prefab;
    public TimelineAsset IntroTimeline;   // 첫 스폰 연출
}
```

### 스폰 경로

| 경로 | 조건 | 동작 |
|------|------|------|
| 일반 스폰 | 세이브 없음 (첫 플레이) | Timeline 재생 → SpawnPoint 도착 → 이벤트 구독 |
| 복원 스폰 | 세이브 있음 | 저장 위치에 직접 스폰 → Timeline 없음 → 복원 완료 후 구독 |

### 게임 종료 시
- 모든 Staff 작업 즉시 취소 (`SchedulingModule.CancelWork()`)
- OrderManager가 해당 OrderTicket을 Waiting 상태로 복귀

---

## 8. GameLoader (신규)

씬 시작 시 세이브 유무를 확인하고 복원 순서를 조율하는 MonoBehaviour.

### 초기화 순서 (세이브 있을 때)

```
1. ObjectManager.LoadStageLayout(saveData)
   → WorkplaceRegisterSO 채워짐 (Workplace들 등록 완료)

2. OrderManager.RestoreTickets(ordersSaveData)
   → OrderTicket 객체 재생성, OrderRegisterSO 등록

3. CustomerManager.RestoreCustomers(ordersSaveData)
   → 티켓 수만큼 Customer 생성
   → WorkplaceRegisterSO에서 빈 Seat 탐색
   → 즉시 이동(텔레포트)·점유

4. OrderManager.LinkTicketsToCustomers()
   → Customer ↔ OrderTicket 연결
   → Phase에 따라 CustomerAgent 상태 설정

5. StaffManager.RestoreStaff(staffSaveData)
   → 저장 위치에 스폰
   → 이벤트 채널 구독 시작

6. UI 갱신
   → OrderTicketUI, AgentStatusUI 등 일괄 반영
```

### 세이브 없을 때
- 각 Manager 정상 초기화 경로 실행 (기존 로직 그대로)

---

## 9. Counter 처리 요약

| 항목 | 방식 |
|------|------|
| 저장 | 일반 Workplace와 동일 (cellIndex로 구분) |
| Staff 위치 | `ObjectData.StaffInteractOffsets`에 카운터 뒤쪽 offset 정의 |
| 에디터 편집 | ObstacleDataEditor StaffInteract 브러시로 시각 편집 |
| 이동 대상 | `CashierWorkSO`가 `Workplace.StaffWorkPoints` 참조 |

---

## 10. 건드려야 하는 파일 목록

### 수정
| 파일 | 변경 내용 |
|------|-----------|
| `ObjectData.cs` | `_id`, `StaffInteractOffsets`, `IconData` 추가, `OnValidate` |
| `Workplace.cs` | `StaffWorkPoints` 추가, `SetupFromObjectData()` 수정 |
| `ObjectManager.cs` | `LoadStageLayout()` save/SO 분기 추가, 배치 시 저장 |
| `ObstacleDataEditor.cs` | `BrushMode.StaffInteract` 추가 |
| `PlayerOrderHandle.cs` | 로드 후 `_readyCount` 재구성 메서드 추가 |
| `CashierWorkSO.cs` | `StaffWorkPoints` 참조로 이동 대상 변경 |
| `CustomerManager.cs` | `RestoreCustomers()` 추가 |
| `OrderManager.cs` | `RestoreTickets()`, `LinkTicketsToCustomers()` 추가 |

### 신규
| 파일 | 내용 |
|------|------|
| `GameSaveData.cs` | 전체 저장 데이터 구조 |
| `ObjectIconDataSO.cs` | 도감 UI용 아이콘·이름·설명 |
| `ObjectDataRegistrySO.cs` | GUID → ObjectData 레지스트리 |
| `StaffConfigSO.cs` | 역할별 프리팹·Timeline 설정 |
| `StaffManager.cs` | Staff 스폰·복원·종료 처리 |
| `GameLoader.cs` | 초기화 순서 조율 |
