# Player Intervention System — Design Spec

**Date:** 2026-05-13
**Branch:** BBJ
**Status:** Approved for implementation

---

## 개요

플레이어가 스태프 전용이었던 작업(요리, 주문받기, 서빙, 계산)을 직접 수행하거나 진행 중인 스태프 작업을 가로채서 처리할 수 있는 시스템.
플레이어는 씬에 물리적 캐릭터가 없으며, 월드 클릭 또는 주문 보드 UI로만 조작한다.

---

## 섹션 1: 핵심 타입 (이미 구현 완료)

### WorkResult

```csharp
public enum WorkResult
{
    Completed,           // 정상 완료
    ExternallyCompleted, // ResolveWork 호출로 외부 완료
    Cancelled            // CancelWork 호출로 강제 취소
}
```

### WorkSO 시그니처

```csharp
public abstract class WorkSO : ScriptableObject
{
    public AgentRole RequiredRole;

    public abstract UniTask<WorkResult> ExecuteAsync(
        ModuleOwner executor, GameEvent context, WorkExecutionContext ctx);

    public virtual void OnResult(
        WorkResult result, IWorkOwner executor, GameEvent context) { }
}
```

`SchedulingModule`이 `ExecuteAsync` 결과를 받아 `OnResult`를 호출한다.

---

## 섹션 2: 소유권 추상화

### 문제

`OrderTicket.TryReserve`, `TryStartProgress`, `OrderManager.NotifyComplete/NotifyReleased` 모두 `ModuleOwner`를 요구한다. 플레이어는 `ModuleOwner`가 없으므로 기존 흐름에 진입 불가.

### 변경

**IWorkOwner (신규)**

```csharp
// Assets/.../Work/IWorkOwner.cs
public interface IWorkOwner { }
```

**ModuleOwner** — `IWorkOwner` 구현 추가 (additive, 기존 코드 영향 없음)

**PlayerWorkOwner (신규, 씬 싱글톤)**

```csharp
// Assets/.../Work/PlayerWorkOwner.cs
public class PlayerWorkOwner : MonoBehaviour, IWorkOwner
{
    public static PlayerWorkOwner Instance { get; private set; }
    private void Awake() => Instance = this;
}
```

**OrderTicket 변경**

```csharp
public IWorkOwner ReservedBy { get; private set; }

public bool TryReserve(IWorkOwner actor)           // ModuleOwner → IWorkOwner
public bool TryStartProgress(IWorkOwner actor)     // ModuleOwner → IWorkOwner

// 신규: 진행 중 강제 소유권 이전 (플레이어 뺏기용)
public bool TrySteal(IWorkOwner newOwner)
{
    if (State == OrderState.Done || State == OrderState.Cancelled) return false;
    ReservedBy = newOwner;
    State = OrderState.Reserved;
    return true;
}
```

**OrderManager 변경**

```csharp
public bool NotifyComplete(OrderTicket ticket, IWorkOwner actor)
public bool NotifyReleased(OrderTicket ticket, IWorkOwner actor)

private static bool IsOwner(OrderTicket ticket, IWorkOwner actor)
    => actor != null && ticket.ReservedBy == actor;
```

**WorkSO.OnResult 시그니처 변경**

```csharp
public virtual void OnResult(WorkResult result, IWorkOwner executor, GameEvent context) { }
```

`SchedulingModule`은 `_owner`를 `IWorkOwner`로 전달 (ModuleOwner가 IWorkOwner를 구현하므로 캐스팅 불필요).

---

## 섹션 3: 플레이어 개입 레이어

### 타입

**IInterventionHandler (신규)**

```csharp
public interface IInterventionHandler
{
    UniTask<WorkResult> HandleAsync(GameEvent context, CancellationToken token);
}
```

**PlayerInterventionSlot (신규)**

```csharp
public class PlayerInterventionSlot
{
    public WorkSO           Work;
    public GameEvent        Context;
    public SchedulingModule ActiveStaff;  // null = 아직 스태프 미배정
}
```

**PlayerInterventionManager (신규)**

```csharp
public class PlayerInterventionManager : MonoBehaviour
{
    public static PlayerInterventionManager Instance { get; private set; }

    public IReadOnlyList<PlayerInterventionSlot> AvailableSlots { get; }
    public event Action OnSlotsChanged;

    public void Claim(PlayerInterventionSlot slot);  // 클레임 + 뺏기 + 핸들러 실행
}
```

### 클레임 흐름 (스태프 진행 중 뺏기 포함)

```
1. Claim(slot) 호출
2. ticket.TrySteal(PlayerWorkOwner.Instance)  → 소유권 이전, State = Reserved
3. slot.ActiveStaff?.CancelWork()
   → 스태프 OnResult: NotifyReleased(ticket, staffOwner)
   → IsOwner 실패 (ReservedBy == playerOwner) → 재dispatch 없음
4. slot.Work.PlayerHandler.HandleAsync(context, token)
5. slot.Work.OnResult(result, PlayerWorkOwner.Instance, context)
   → NotifyComplete(ticket, PlayerWorkOwner) → IsOwner 성공 → 정상 처리
```

---

## 섹션 4: 핸들러 등록 구조

**InterventionHandlerSO (신규, abstract SO)**

```csharp
public abstract class InterventionHandlerSO : ScriptableObject, IInterventionHandler
{
    public abstract UniTask<WorkResult> HandleAsync(GameEvent context, CancellationToken token);
}
```

**ImmediateInterventionHandlerSO (신규, 기본 TBD 구현)**

```csharp
[CreateAssetMenu(menuName = "Tycoon/Intervention/Immediate")]
public class ImmediateInterventionHandlerSO : InterventionHandlerSO
{
    public override UniTask<WorkResult> HandleAsync(GameEvent context, CancellationToken token)
        => UniTask.FromResult(WorkResult.Completed);
}
```

**WorkSO 추가 필드**

```csharp
[SerializeField] private InterventionHandlerSO _playerHandler;
public bool IsPlayerInteractable => _playerHandler != null;
public InterventionHandlerSO PlayerHandler => _playerHandler;
```

인스펙터에서 연결:
- `CookWorkSO._playerHandler` = `CookMinigameHandlerSO` (나중에 구현)
- `TakeOrderWorkSO._playerHandler` = `ImmediateInterventionHandlerSO` (TBD)
- `ServeWorkSO._playerHandler` = `ImmediateInterventionHandlerSO` (TBD)
- `CashierWorkSO._playerHandler` = `ImmediateInterventionHandlerSO` (TBD)

---

## 섹션 5: 슬롯 감지 및 UI 트리거

### 슬롯 감지

`PlayerInterventionManager`가 `OrderRegisterSO`와 이벤트 채널을 구독해 슬롯 목록을 유지.

```csharp
// 구독 대상
_orderChannel: OrderRegisteredEvent, OrderStateChangedEvent, OrderUnregisteredEvent
_scheduleTriggerChannel: ScheduleTriggerEvent

// RebuildSlots() 로직
foreach ticket in _orderRegister.Registry:
    entry = _dispatchTable.FindEntry(ticket.WorkPhase)
    if entry.Work.IsPlayerInteractable:
        activeStaff = FindStaffWorkingOn(ticket)
        _slots.Add(new PlayerInterventionSlot(entry.Work, new OrderWorkEvent(ticket, ...), activeStaff))

FindStaffWorkingOn(ticket):
    if ticket.ReservedBy is ModuleOwner owner:
        return owner.GetModule<SchedulingModule>()
    return null
```

`SchedulingModule`에 추가 노출:

```csharp
public WorkSO    CurrentWork    { get; private set; }
public GameEvent CurrentContext { get; private set; }
// AssignWork 시 set, RunAsync finally 블록에서 clear
```

### UI 트리거 1: 월드 클릭

```csharp
// IPlayerInteractable.cs (신규)
public interface IPlayerInteractable
{
    bool CanPlayerInteract { get; }
    void OnPlayerClick();
}

// Workplace.cs 에 추가
// CanPlayerInteract: AvailableSlots 중 이 Workplace가 target인 slot 존재 여부
// OnPlayerClick: 해당 slot 찾아 PlayerInterventionManager.Instance.Claim(slot)
```

클릭 감지는 기존 입력 시스템 또는 씬의 `PlayerInputHandler`가 담당 (별도 구현).

### UI 트리거 2: 주문 보드 UI

```csharp
// OrderTicketUI.cs 에 추가
// [SerializeField] Button _claimButton;
// 활성화 조건: AvailableSlots에 해당 ticket의 slot 존재
// 클릭: PlayerInterventionManager.Instance.Claim(slot)
```

---

## 변경 범위 요약

| 파일 | 변경 종류 |
|------|----------|
| `IWorkOwner.cs` | 신규 |
| `PlayerWorkOwner.cs` | 신규 |
| `InterventionHandlerSO.cs` | 신규 |
| `ImmediateInterventionHandlerSO.cs` | 신규 |
| `IInterventionHandler.cs` | 신규 |
| `PlayerInterventionSlot.cs` | 신규 |
| `PlayerInterventionManager.cs` | 신규 |
| `IPlayerInteractable.cs` | 신규 |
| `ModuleOwner.cs` | `: IWorkOwner` 추가 |
| `WorkSO.cs` | `_playerHandler` 필드, `OnResult` 시그니처 변경 |
| `OrderTicket.cs` | `IWorkOwner` 타입 변경, `TrySteal` 추가 |
| `OrderManager.cs` | `IWorkOwner` 시그니처 변경 |
| `SchedulingModule.cs` | `CurrentWork/CurrentContext` 노출 |
| `Workplace.cs` | `IPlayerInteractable` 구현 |
| `OrderTicketUI.cs` | claim 버튼 추가 |
| 기존 WorkSO들 (`OnResult`) | `ModuleOwner` → `IWorkOwner` 파라미터 변경 |

기존 `ExecuteAsync` 내부 로직 변경 없음. 기존 스태프 흐름 동작 보장.

---

## 미구현 (TBD, 구조만 선언)

- 각 WorkSO 미니게임/인터랙션 `InterventionHandlerSO` 구체 구현
- `PlayerInputHandler` (월드 클릭 감지)
- `Workplace.CanPlayerInteract` 매칭 로직 상세
- `TakeOrder` 전용 처리 (티켓 없는 상태에서의 뺏기)
