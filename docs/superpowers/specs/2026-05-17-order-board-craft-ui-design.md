# 주문판 UI (조리 스테이션) 설계 문서

작성일: 2026-05-17

---

## 개요

플레이어가 조리 스테이션에서 `PendingCook` 단계 주문을 확인하고 점유하여 미니게임을 시작하는 UI 시스템.  
Agent가 이미 점유한 주문을 뺏거나, 미점유 주문을 선점해 다른 Agent가 접근하지 못하도록 잠근다.

---

## 범위

- `PlayerOrderHandle` : 플레이어 `ModuleOwner` 핸들, 조리 단계 티켓 관리
- `OrderBoardCraftUI` : 조리 주문 보드 패널
- `FoodGroupCardUI` : 음식 종류별 카드 (미점유 / 점유 두 종류)
- `StealConfirmUI` : 점유 뺏기 확인 팝업
- `PlayerCraftStartEvent` : 점유 완료 후 미니게임 시작 신호

기존 `OrderManager`, `OrderTicket`, `EventChannelSO`, `OrderRegisterSO`는 변경하지 않는다.

---

## 데이터 흐름

```
OrderChannel
  └─ OrderRegisteredEvent / OrderUnregisteredEvent / OrderStateChangedEvent
       ├─ PlayerOrderHandle  →  _pendingTickets, _readyCount 동기화
       └─ OrderBoardCraftUI  →  카드 추가 / 제거 / 갱신
```

미니게임 완료 시 외부에서 `PlayerOrderHandle.AddReadyFood(food)` 호출 → `_readyCount` 증가.  
서빙 완료 시 `_readyCount` 감소.

---

## 클래스 설계

### PlayerOrderHandle : MonoBehaviour

플레이어가 OrderTicket을 점유하고 뺏기 위한 핸들.  
`PendingCook` 단계 티켓을 음식 종류별로 관리하며 조리 완료 재고도 추적한다.

```
[SerializeField] ModuleOwner     _playerOwner
[SerializeField] EventChannelSO  _orderChannel

Dictionary<FoodDataSO, int>               _readyCount      // 조리 완료 재고
Dictionary<FoodDataSO, List<OrderTicket>> _pendingTickets  // PendingCook 티켓
```

**이벤트 구독** (OnEnable / OnDisable):
- `OrderRegisteredEvent` → WorkPhase == PendingCook이면 `_pendingTickets`에 추가
- `OrderUnregisteredEvent` → `_pendingTickets`에서 제거
- `OrderStateChangedEvent` → WorkPhase 변경 시 `_pendingTickets` 재분류

**쿼리**:
- `GetFreeTicket(FoodDataSO)` → State == Waiting 티켓 1개 반환
- `GetOccupiedTicket(FoodDataSO)` → State == Reserved/InProgress 티켓 1개 반환
- `GetPendingCount(FoodDataSO)` → 전체 PendingCook 티켓 수
- `HasFreeTicket(FoodDataSO)`, `HasOccupiedTicket(FoodDataSO)`

**액션**:
- `TryOccupy(ticket)` → `ticket.TryReserve(_playerOwner)`
- `TrySteal(ticket)` → `ticket.TrySteal(_playerOwner)` + `RaiseEvent(OrderStateChangedEvent)`
- `AddReadyFood(food)` → `_readyCount[food]++`
- `ConsumeReadyFood(food)` → `_readyCount[food]--`

---

### OrderBoardCraftUI : MonoBehaviour

PendingCook 티켓을 음식 종류별로 묶어 표시하는 보드 패널.

```
[SerializeField] PlayerOrderHandle  _handle
[SerializeField] FoodGroupCardUI    _cardPrefab
[SerializeField] StealConfirmUI     _confirmUI
[SerializeField] EventChannelSO     _orderChannel
[SerializeField] Transform          _content
[SerializeField] Button             _cancelButton

Dictionary<FoodDataSO, FoodGroupCardUI> _freeCards      // 미점유 카드 (food → 카드)
Dictionary<FoodDataSO, FoodGroupCardUI> _occupiedCards  // 점유 카드 (food → 카드)
```

**카드 표시 규칙**:
- 같은 음식의 Waiting 티켓이 1개 이상 → `_freeCards`에 카드 1개, count 표시
- 같은 음식의 Reserved/InProgress 티켓이 1개 이상 → `_occupiedCards`에 카드 1개, "점유" 표시 (여러 개여도 카드는 1개, 뺏기 대상은 `GetOccupiedTicket`이 반환하는 첫 번째 티켓)
- 두 종류 동시에 존재 가능: "맥주 x2" 카드 + "맥주 (점유)" 카드

**인터랙션 흐름**:
```
[미점유 카드 클릭]
  ticket = _handle.GetFreeTicket(food)
  _handle.TryOccupy(ticket) → 성공
  _orderChannel.RaiseEvent(PlayerCraftStartEvent(ticket))
  Close()

[점유 카드 클릭]
  _confirmUI.Show(ticket)
    ├─ [승낙] → _handle.TrySteal(ticket)
    │           _orderChannel.RaiseEvent(PlayerCraftStartEvent(ticket))
    │           Close()
    └─ [취소] → _confirmUI 닫기만 (보드 유지)

[ESC / 취소 버튼]
  Close()   // 점유 없었으므로 별도 release 불필요
```

**이벤트 구독** (Open / Close 시 등록 / 해제):
- `OrderRegisteredEvent` → 해당 음식 카드 생성 또는 count 갱신
- `OrderUnregisteredEvent` → count 감소, 0이면 카드 제거
- `OrderStateChangedEvent` → Waiting ↔ Reserved 전환 시 freeCard / occupiedCard 재분류

---

### FoodGroupCardUI : MonoBehaviour

```
[SerializeField] Image    _foodIcon
[SerializeField] TMP_Text _foodName
[SerializeField] TMP_Text _countLabel    // "x2" 또는 ""
[SerializeField] Image    _occupiedBadge // 점유 시 활성화
[SerializeField] Button   _button

void Setup(FoodDataSO food, int count, bool isOccupied, Action<FoodDataSO> onClick)
void UpdateCount(int count)
void SetOccupied(bool occupied)
```

- `isOccupied == false` : `_countLabel` = "x{count}", `_occupiedBadge` 비활성
- `isOccupied == true`  : `_countLabel` 비활성, `_occupiedBadge` 활성

---

### StealConfirmUI : MonoBehaviour

```
[SerializeField] TMP_Text _messageLabel
[SerializeField] Button   _confirmButton
[SerializeField] Button   _cancelButton

void Show(OrderTicket ticket, Action onConfirm, Action onCancel)
void Hide()
```

메시지: "이미 점유 중입니다. 작업을 빼앗겠습니까?"

---

### PlayerCraftStartEvent (OrderEvents.cs에 추가)

```csharp
public class PlayerCraftStartEvent : GameEvent
{
    public OrderTicket Ticket { get; }
    public PlayerCraftStartEvent(OrderTicket ticket) => Ticket = ticket;
}
```

---

## 파일 위치

```
Assets/00. Work/BBJ/02. Scripts/
  Order/
    PlayerOrderHandle.cs          (신규)
  Event/
    OrderEvents.cs                (PlayerCraftStartEvent 추가)
  UI/Order/
    OrderBoardCraftUI.cs          (신규)
    FoodGroupCardUI.cs            (신규)
    StealConfirmUI.cs             (신규)
```

---

## 검증 방법 (Unity Editor)

1. PendingCook 티켓 2개 (같은 음식) 생성 → "맥주 x2" 카드 1개 확인
2. Agent가 1개 점유 → "맥주 x1" + "맥주 (점유)" 두 카드로 분리 확인
3. 점유 카드 클릭 → StealConfirmUI 표시 확인
4. 승낙 → `PlayerCraftStartEvent` 발행 + 보드 닫힘 확인
5. 취소 → 보드 유지 확인
6. ESC → 보드 닫힘, 점유 없으면 release 없음 확인
