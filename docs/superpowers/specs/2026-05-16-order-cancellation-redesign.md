# Order Cancellation Redesign

**Date:** 2026-05-16  
**Branch:** BBJ  
**Scope:** Order/Work/Schedule 취소 흐름 통합 및 책임 분리

---

## 문제

현재 취소 로직이 최소 4곳에서 각자 다른 방식으로 발생하고 있어 버그 가능성이 높고 유지보수가 어렵다.

- `CustomerAgent.ResetItem()` → `ActiveTicket = null` 직접 처리 (OrderManager 모름)
- `WaitOrderWorkSO.OnResult(ExternallyCompleted)` → 서버 CancelWork 직접 호출
- `OrderManager.RegisterClear()` → 씬 언로드 시 일괄 취소
- `OrderManager.CancelOrder()` → 이벤트 채널 경유 취소

추가 문제:
- `GameEvent context`를 매 WorkSO에서 캐스팅 (타입 불안전)
- `OnResult`가 `RunAsync`와 분리되어 실행 결과 처리가 두 곳에 존재
- `WorkSO`가 `CustomerAgent.AssignedServer`, `ISchedulable` 등 다른 시스템을 직접 참조

---

## 설계 원칙

세 가지 책임을 분리한다:

1. **작업 실행** — WorkSO가 어떻게 일을 하는가
2. **주문 단계 전환** — OrderManager가 단계를 어떻게 진행시키는가
3. **플레이어 개입** — 입력 핸들러가 정상 흐름을 어떻게 가로채는가

---

## 섹션 1: `OrderTicket` 변경

`OrderTicket`이 `CancellationTokenSource`를 소유한다. 취소 시 연결된 모든 워커의 UniTask가 자동으로 중단된다.

```csharp
public class OrderTicket
{
    // 기존 프로퍼티 유지

    private readonly CancellationTokenSource _cts = new();
    public CancellationToken Token => _cts.Token;
    public bool IsTerminal => State is OrderState.Done or OrderState.Cancelled;

    internal void Cancel(CancelReason reason)
    {
        CancellationReason = reason;
        ReservedBy = null;
        State = OrderState.Cancelled;
        _cts.Cancel();
        _cts.Dispose();
    }

    internal void Finish()
    {
        ReservedBy = null;
        State = OrderState.Done;
        _cts.Dispose();
    }
}
```

**변경 파일:** `OrderTicket.cs`  
**추가:** `Token`, `IsTerminal`, CTS 소유  
**변경:** `Cancel()`, `Finish()` 내부에 CTS 처리 추가

---

## 섹션 2: `WorkSO` — `OnResult` 제거, RunAsync 단일 책임

모든 WorkSO에서 `OnResult` 오버라이드를 제거하고 `RunAsync` 안에서 linked token + try/catch/finally로 모든 케이스를 처리한다.

### 패턴

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
{
    var ev = context as OrderWorkEvent;
    if (ev == null) return WorkResult.Cancelled;
    if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
        ctx.Token, ev.Ticket.Token);

    try
    {
        // 작업 수행 (linked.Token 사용)
        await actions.Execute<MoveAction>(a => a.ExecuteAsync(..., linked.Token));

        _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ev.Ticket, executor));
        return WorkResult.Completed;
    }
    catch (OperationCanceledException)
    {
        // IsTerminal이면 OrderManager가 이미 처리 → 아무것도 안 함
        if (!ev.Ticket.IsTerminal)
            _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ev.Ticket, executor));
        return WorkResult.Cancelled;
    }
    finally
    {
        // 점유, 모듈 등 항상 정리
    }
}
```

### `WaitOrderWorkSO` 특수 케이스

`WaitOrderWorkSO`는 손님이 주문을 기다리는 단계이므로 **이 시점에 `OrderTicket`은 아직 존재하지 않는다.** linked token 대신 `ctx.Token`만 사용한다.

타임아웃 시 OrderTicket 취소가 아니라, 담당 서버의 작업을 취소하고 손님 상태를 원복한다:

```csharp
// WaitOrderWorkSO — OnResult 제거 후
try
{
    customer.SetAwaitingOrder(true);
    _ctx.DispatchTable?.Dispatch(OrderWorkPhase.ReadyForServer, new TakeOrderEvent(seat));
    float deadline = Time.time + _patienceLimit;

    await actions.Execute<WaitAction>(a => a.ExecuteAsync(
        () => customer.OrderPlaced || Time.time >= deadline, ctx.Token));

    // 타임아웃: 담당 서버 작업 취소 (아직 티켓 없음)
    if (!customer.OrderPlaced)
        customer.AssignedServer?.GetModule<ISchedulable>()?.CancelWork();

    return WorkResult.Completed;
}
finally
{
    customer.SetAwaitingOrder(false);
}
```

> **참고:** 타임아웃 후 손님이 자리를 떠나면 `ResetItem`이 호출되고, 그 시점에 `ActiveTicket`이 있으면 섹션 5의 흐름을 탄다.

### 제거 목록

| 항목 | 위치 |
|---|---|
| `OnResult` 오버라이드 | `CookWorkSO`, `ServeWorkSO`, `CashierWorkSO`, `WaitOrderWorkSO` |
| `WorkResult.ExternallyCompleted` | `WorkResult.cs` |
| `WorkExecutionContext.WasExternallyCompleted` | `WorkExecutionContext.cs` |
| `ctx.Pause()` 외부 완료 신호 관련 코드 | `WorkExecutionContext.cs` |

> **주의:** `ctx.Pause()` / `WaitIfPausedAsync()`의 미니게임 일시정지 기능은 유지. 외부 완료 신호 목적의 코드만 제거.

**변경 파일:** `CookWorkSO.cs`, `ServeWorkSO.cs`, `CashierWorkSO.cs`, `WaitOrderWorkSO.cs`, `WorkResult.cs`, `WorkExecutionContext.cs`

---

## 섹션 3: `OrderManager` — 변경 최소화

`CancelOrder`에서 `IsTerminal` 체크로 교체. 나머지 로직(`NotifyComplete`, `NotifyReleased`, `HandleInterrupted`)은 그대로.

```csharp
public void CancelOrder(OrderTicket ticket, CancelReason reason)
{
    if (ticket.IsTerminal) return;  // 기존 State 비교에서 교체

    ticket.Cancel(reason);          // Cancel() 내부에서 _cts.Cancel() 전파
    _orderRegister.Unregister(ticket);
    _orderChannel?.RaiseEvent(new OrderUnregisteredEvent(ticket));
}
```

`RegisterClear`는 변경 없음 — `ticket.Cancel()` 호출이 이미 CTS 발화를 포함하게 되므로 자동으로 동작.

**변경 파일:** `OrderManager.cs` (최소 변경)

---

## 섹션 4: 플레이어 개입 — 입력 핸들러의 책임

기존에 `WaitOrderWorkSO.OnResult`가 담당하던 두 행동을 플레이어 입력 핸들러로 이전:

```csharp
private void OnCustomerClicked(CustomerAgent customer)
{
    if (!customer.IsReadyForOrder) return;
    if (customer.OrderPlaced) return;

    // 1. 담당 서버 작업 취소
    customer.AssignedServer?.GetModule<ISchedulable>()?.CancelWork();

    // 2. 주문 직접 등록
    var ticket = customer.PlaceOrder(customer.AssignedSeat);
    if (ticket != null)
        _orderChannel.RaiseEvent(new OrderTicketRegisterEvent(ticket));
}
```

`WaitOrderWorkSO`는 `customer.OrderPlaced == true`를 감지하면 자연 종료. 누가 주문을 넣었는지 알 필요 없음.

**변경 파일:** 기존 플레이어 개입 핸들러 (파일명 확인 필요)

---

## 섹션 5: `CustomerAgent.ResetItem` — OrderManager 경유

```csharp
public override void ResetItem()
{
    if (ActiveTicket != null && !ActiveTicket.IsTerminal)
        _orderChannel?.RaiseEvent(
            new OrderCancelRequestEvent(ActiveTicket, CancelReason.CustomerLeft));

    GetModule<SchedulingModule>()?.CancelWork();
    AssignedSeat    = null;
    AssignedServer  = null;
    ActiveTicket    = null;
    SelectedFood    = null;
    OrderPlaced     = false;
    FoodServed      = false;
    PaymentDone     = false;
    IsAwaitingOrder = false;
}
```

이벤트 채널 → `OrderManager.CancelOrder` → `ticket.Cancel()` → `_cts.Cancel()` → 진행 중인 워커까지 전파.

**변경 파일:** `CustomerAgent.cs`

---

## 전체 변경 요약

| 파일 | 변경 내용 |
|---|---|
| `OrderTicket.cs` | CTS 추가, `IsTerminal`, `Cancel()`/`Finish()` CTS 처리 |
| `CookWorkSO.cs` | `OnResult` 제거, linked token + try/catch/finally |
| `ServeWorkSO.cs` | `OnResult` 제거, linked token + try/catch/finally |
| `CashierWorkSO.cs` | `OnResult` 제거, linked token + try/catch/finally |
| `WaitOrderWorkSO.cs` | `OnResult` 제거, 타임아웃 인라인 처리 |
| `WorkSO.cs` | `virtual OnResult` 제거 |
| `WorkResult.cs` | `ExternallyCompleted` 제거 |
| `WorkExecutionContext.cs` | 외부 완료 신호 관련 제거 (pause 기능 유지) |
| `OrderManager.cs` | `IsTerminal` 체크 교체 (최소 변경) |
| `CustomerAgent.cs` | `ResetItem`에서 이벤트 채널 경유 취소 |
| 플레이어 개입 핸들러 | 서버 취소 + 주문 등록 직접 처리 |

---

## 검증 방법 (Unity 에디터)

1. 손님 스폰 → 서버가 이동 중에 손님을 클릭 → 주문 등록되고 서버가 멈춤
2. 손님 스폰 → 요리 중에 손님 강제 퇴장 → 요리사 작업 취소되고 레지스트리 정리됨
3. 씬 언로드 → 진행 중인 모든 주문 Cancelled 상태로 정리됨
4. 정상 흐름 → 주문 → 요리 → 서빙 → 결제 → Done 상태 확인
