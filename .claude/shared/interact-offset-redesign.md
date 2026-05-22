# InteractOffset 재설계 설계서

## 배경 및 문제

기존 `TileSetData`에 `InteractOffsets`(손님용)과 `StaffInteractOffsets`(직원용)을 별도 배열로 보유.
에디터에서 독립적으로 페인팅하다 보니 같은 타일이 양쪽에 모두 등록되는 실수가 발생.
(예: 의자에서 손님 착석 위치와 직원 서빙 위치가 동일 좌표를 가리킴)

---

## 확정된 설계

### 1. 데이터 구조

#### `TileSetData` SO (그리드 전용으로 축소)
- `Vector2Int[] BlockedOffsets` — 이동 불가 셀
- `bool IsWalkable` — 루트 셀 보행 가능 여부
- ~~InteractOffsets~~ / ~~StaffInteractOffsets~~ 제거

#### `InteractRoleSO` (신규 SO)
- `Color GizmoColor` — 에디터 Gizmo/씬 뷰 표시 색상
- SO 자체가 역할 식별 키 (참조 비교)

#### `InteractPoint` struct
```csharp
[Serializable]
struct InteractPoint
{
    public Vector2Int Offset;
    public InteractRoleSO Role;
}
```

#### `Workplace` 컴포넌트 (신규 필드)
```csharp
[SerializeField] TileSetData _tileSetData;
[SerializeField] List<InteractPoint> _interactPoints;
[SerializeField] bool _flipX;
```
- `_flipX` 적용 시 `InteractPoints`와 `BlockedOffsets` 모두 X축 반전

---

### 2. ObjectData 계층

#### `ObjectDataSO`
- `string Id` — GUID (TileSetData에서 이전)
- `Sprite Icon`, `string DisplayName`, `string Description`
- `GameObject WorkplacePrefab` — Workplace 컴포넌트를 포함한 프리팹

#### `ObjectDataRegistrySO`
- `ObjectDataSO`를 Id로 인덱싱

#### 오브젝트 배치 흐름
```
ObjectDataSO.WorkplacePrefab
    → 인스턴스화
    → Workplace.TileSetData로 GridManager.ApplyObstacleAt()
    → Workplace._interactPoints로 인터랙션 포인트 초기화
```

---

### 3. Workplace API

```csharp
// 역할 SO를 키로 가장 가까운 포인트 반환
public Vector3 GetNearestPoint(InteractRoleSO role, Vector3 from);
```

각 WorkSO가 자신의 역할 SO를 보유:
```csharp
// CashierWorkSO, TakeSeatWorkSO 등
[SerializeField] InteractRoleSO _role;

// 호출
workplace.GetNearestPoint(_role, executor.transform.position);
```

---

### 4. Flip 기능

- `Workplace._flipX` — 인스펙터에서 배치 시 설정, 이후 고정
- 저장값에 포함: `PlacedObstacleEntrySave.FlipX`
- 적용 대상: `InteractPoints` 오프셋 + `TileSetData.BlockedOffsets` 모두 X축 반전

---

### 5. 에디터

| 대상 | 편집 방법 |
|------|-----------|
| `InteractPoints` | Workplace 선택 시 씬 뷰 핸들로 타일 클릭 추가/제거, 역할은 인스펙터에서 선택 |
| `BlockedOffsets` | 기존 `ObstacleDataEditor` (TileSetData SO 에디터) 유지 |
| Gizmo | Workplace가 `OnDrawGizmos`에서 `Role.GizmoColor`로 포인트 표시 |

---

### 6. 저장

```csharp
class PlacedObstacleEntrySave
{
    public string     ObjectDataId; // ObjectDataSO.Id
    public Vector2Int CellIndex;
    public bool       FlipX;        // 신규 추가
}
```

---

## 변경 파일 목록

| 파일 | 변경 내용 |
|------|-----------|
| `TileSetData.cs` | `InteractOffsets`, `StaffInteractOffsets` 제거 |
| `Workplace.cs` | `_interactPoints`, `_tileSetData`, `_flipX` 추가; `GetNearestPoint(role)` API 변경 |
| `ObjectDataSO.cs` | `Id` 추가, `TileSet` → `WorkplacePrefab`으로 교체 |
| `ObjectDataRegistrySO.cs` | ObjectDataSO 기준으로 인덱싱 변경 |
| `ObjectManager.cs` | `PlaceObjectInternal` — TileSetData 접근 경로 변경 |
| `CashierWorkSO.cs` 외 WorkSO | `InteractRoleSO _role` 추가, `GetNearestPoint` 호출 변경 |
| `GameSaveData.cs` | `PlacedObstacleEntrySave.FlipX` 추가 |
| `InteractRoleSO.cs` | 신규 생성 |
| `ObstacleDataEditor.cs` | InteractOffset 관련 UI 제거 |
| `Workplace` Editor | 씬 뷰 핸들 Custom Editor 신규 작성 |
