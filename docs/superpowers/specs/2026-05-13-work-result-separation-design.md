# Work Result Separation — Design Spec

**Date:** 2026-05-13
**Branch:** BBJ
**Status:** Approved

---

## 문제

현재 WorkSO 구현체는 **실행(이동, 대기)**과 **결과 처리(상태 변경)** 를 하나의 `ExecuteAsync` 안에 섞어 놓았다.

```csharp
// ServeWorkSO — 현재
catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
{
    ev.OrderManager.NotifyComplete(ev.Ticket, executor); // 상태 변경
    customer?.OnFoodServed();                            // 상태 변경
}
```

이로 인한 문제:
1. **CustomerCycleSequenceSO가 ExternallyCompleted 시 시퀀스를 조기 종료** → 손님이 착석 후 아무것도 안 함
2. **CashierWorkSO null slot 버그** → slot이 없는 상태에서 NotifyComplete 호출 가능
3. **OnPlayerInteract 이중 호출** → ResolveWork와 OnFoodServed 직접 호출이 동시에 실행됨
4. **각 경로(정상/외부완료/취소)마다 상태 변경 코드가 분산** → 추적·수정 어려움

---

## 목표

- WorkSO.ExecuteAsync = **실행만** (이동, 타이머, DoWorkAsync, 취소 시 리소스 해제)
- 도메인 상태 변경 = **WorkSO.OnResult** (NotifyComplete, OnFoodServed 등)
- SchedulingModule이 두 단계를 순서대로 호출
- CustomerCycleSequenceSO는 ExternallyCompleted에서도 시퀀스를 계속 진행

---

## 핵심 설계

### WorkResult enum (신규)

```csharp
// Assets/00. Work/BBJ/02. Scripts/Work/WorkResult.cs
public enum WorkResult
{
    Completed,           // 정상 완료
    ExternallyCompleted, // ResolveWork() 호출로 외부 완료
    Cancelled            // CancelWork() 호출로 강제 취소
}
```

### WorkSO 변경

```csharp
public abstract class WorkSO : ScriptableObject
{
    public AgentRole RequiredRole;

    // 실행: 이동, 대기, DoWorkAsync, 취소 시 리소스 해제
    public abstract UniTask<WorkResult> ExecuteAsync(
        ModuleOwner executor, GameEvent context, WorkExecutionContext ctx);

    // 결과 처리: NotifyComplete, OnFoodServed 등 도메인 상태 변경
    // 기본 구현 = 빈 메서드
    public virtual void OnResult(
        WorkResult result, ModuleOwner executor, GameEvent context) { }
}
```

### ExecuteAsync 패턴 (모든 WorkSO 공통)

```csharp
try
{
    // 이동 + 작업
    return WorkResult.Completed;
}
catch (OperationCanceledException)
{
    // 취소 시 리소스 해제 (occupancy release 등)
    return ctx.WasExternallyCompleted
        ? WorkResult.ExternallyCompleted
        : WorkResult.Cancelled;
}
```

---

## SchedulingModule 변경

```csharp
private async UniTaskVoid RunAsync(WorkSO workSO, GameEvent context, WorkExecutionContext ctx)
{
    OnWorkStarted?.Invoke();
    WorkResult result = WorkResult.Cancelled;
    try
    {
        result = await workSO.ExecuteAsync(_owner, context, ctx);
    }
    catch (OperationCanceledException)
    {
        result = ctx.WasExternallyCompleted
            ? WorkResult.ExternallyCompleted
            : WorkResult.Cancelled;
    }
    finally
    {
        if (_execCtx == ctx) _execCtx = null;
        ctx.Dispose();
        workSO.OnResult(result, _owner, context);
        OnWorkEnded?.Invoke(result != WorkResult.Cancelled);
        ScheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());
    }
}
```

`OnWorkEnded` 시그니처(`Action<bool>`)는 변경 없음.

---

## CustomerCycleSequenceSO 변경

**핵심 변화**: ExternallyCompleted를 받아도 시퀀스를 종료하지 않고 다음 스텝으로 진행.

```csharp
public override async UniTask<WorkResult> ExecuteAsync(
    ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
{
    if (_steps == null) return WorkResult.Completed;

    foreach (var step in _steps)
    {
        if (step == null) continue;

        WorkResult stepResult;
        try
        {
            stepResult = await step.ExecuteAsync(executor, context, ctx);
        }
        catch (OperationCanceledException)
        {
            stepResult = ctx.WasExternallyCompleted
                ? WorkResult.ExternallyCompleted
                : WorkResult.Cancelled;
        }

        step.OnResult(stepResult, executor, context);

        if (stepResult == WorkResult.Cancelled)          return WorkResult.Cancelled;
        if (stepResult == WorkResult.ExternallyCompleted) return WorkResult.Completed;
        // Completed → 다음 스텝으로 계속
        // ExternallyCompleted: ctx.Token이 이미 취소됐으므로 다음 스텝 실행 불가
        // → Completed 반환으로 시퀀스를 성공 처리하고 종료
    }

    return WorkResult.Completed;
}
```

CustomerCycleSequenceSO는 자체 `OnResult`를 override하지 않음 (빈 base 그대로).

---

## WorkSO 구현체별 변경

### ServeWorkSO

```csharp
public override async UniTask<WorkResult> ExecuteAsync(
    ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
{
    var agent = executor as IActionDispatcher;
    var ev    = context as OrderWorkEvent;

    if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

    var serveStation = _workplaceRegister?.GetFirst(_serveStationTypeSO);
    if (serveStation == null) return WorkResult.Cancelled;

    Vector3 from = executor.transform.position;
    try
    {
        await agent.MoveAsync(serveStation.GetNearestPoint(from), ctx.Token);
        ctx.Token.ThrowIfCancellationRequested();
        ev.Ticket.TryStartProgress(executor);

        await agent.MoveAsync(ev.Ticket.Seat.GetNearestPoint(from), ctx.Token);
        ctx.Token.ThrowIfCancellationRequested();
        await agent.DoWorkAsync(ev.Ticket.Seat, ctx.Token);
        ctx.Token.ThrowIfCancellationRequested();

        return WorkResult.Completed;
    }
    catch (OperationCanceledException)
    {
        return ctx.WasExternallyCompleted
            ? WorkResult.ExternallyCompleted
            : WorkResult.Cancelled;
    }
}

public override void OnResult(WorkResult result, ModuleOwner executor, GameEvent context)
{
    var ev       = context as OrderWorkEvent;
    var customer = ev.Ticket.Seat.GetModule<SeatModule>()?.AssignedAgent as CustomerAgent;

    if (result != WorkResult.Cancelled)
    {
        ev.OrderManager.NotifyComplete(ev.Ticket, executor);
        customer?.OnFoodServed();
    }
    else
    {
        ev.OrderManager.NotifyReleased(ev.Ticket, executor);
    }
}
```

### CookWorkSO

```csharp
public override async UniTask<WorkResult> ExecuteAsync(...)
{
    // 기존 이동+작업 코드 유지
    try
    {
        await agent.MoveAsync(..., ctx.Token);
        ev.Ticket.TryStartProgress(actor);
        foodContext?.SetFood(ev.Ticket.Food);
        await agent.DoWorkAsync(kitchen, ctx.Token);
        return WorkResult.Completed;
    }
    catch (OperationCanceledException)
    {
        return ctx.WasExternallyCompleted
            ? WorkResult.ExternallyCompleted
            : WorkResult.Cancelled;
    }
    finally
    {
        foodContext?.ClearFood();
        kitchen.GetModule<OccupancyModule>()?.Release();
    }
}

public override void OnResult(WorkResult result, ModuleOwner executor, GameEvent context)
{
    var ev = context as OrderWorkEvent;
    if (result != WorkResult.Cancelled)
        ev.OrderManager.NotifyComplete(ev.Ticket, executor);
    else
        ev.OrderManager.NotifyReleased(ev.Ticket, executor);
}
```

### CashierWorkSO (null slot 버그 수정)

`slot.NotifyProcessed()`는 실행 완료 시점(try 블록 마지막)에 직접 호출합니다.
SO 공유 인스턴스 문제를 피하기 위해 인스턴스 필드를 사용하지 않습니다.

```csharp
public override async UniTask<WorkResult> ExecuteAsync(...)
{
    if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;
    // ...
    try
    {
        await agent.MoveAsync(..., ctx.Token);
        ev.Ticket.TryStartProgress(executor);
        await agent.WaitUntilAsync(() => queue.HasWaiting, ctx.Token);

        var slot = queue.Dequeue();
        if (slot == null) return WorkResult.Cancelled;

        await agent.DoWorkAsync(counter, ctx.Token);
        ctx.Token.ThrowIfCancellationRequested();

        slot.Value.NotifyProcessed(); // 정상 완료 시에만 호출
        return WorkResult.Completed;
    }
    catch (OperationCanceledException)
    {
        // ExternallyCompleted 시 slot이 없거나 작업 미완료 → NotifyProcessed 호출 안 함
        return ctx.WasExternallyCompleted
            ? WorkResult.ExternallyCompleted
            : WorkResult.Cancelled;
    }
}

public override void OnResult(WorkResult result, ModuleOwner executor, GameEvent context)
{
    var ev = context as OrderWorkEvent;
    if (result != WorkResult.Cancelled)
        ev.OrderManager.NotifyComplete(ev.Ticket, executor);
    else
        ev.OrderManager.NotifyReleased(ev.Ticket, executor);
}
```

### TakeSeatWorkSO, PayAtCounterWorkSO

`OnResult` override 불필요. 결과 처리가 이미 실행 흐름 내부(착석 배정, slot 콜백)에 완결됨.

### 나머지 WorkSO 10개 (EatWorkSO, WaitForFoodWorkSO, WaitOrderWorkSO, ExitWorkSO, TakeOrderWorkSO, AgentActionWorkSO 등)

반환형만 `UniTask<WorkResult>`로 변경. catch 블록에서 `return WorkResult.Cancelled` / `return WorkResult.ExternallyCompleted` 패턴 적용.

---

## CustomerAgent.OnPlayerInteract 수정

```csharp
// 변경 전 (버그: OnFoodServed 이중 호출 가능)
public void OnPlayerInteract()
{
    ActiveTicket?.ReservedBy?.GetModule<SchedulingModule>()?.ResolveWork();
    if (OrderPlaced && !FoodServed) OnFoodServed();
}

// 변경 후
public void OnPlayerInteract()
{
    ActiveTicket?.ReservedBy?.GetModule<SchedulingModule>()?.ResolveWork();
}
```

---

## 영향 범위 요약

| 파일 | 변경 내용 |
|---|---|
| `WorkResult.cs` | **신규** — enum |
| `WorkSO.cs` | 반환형 `UniTask<WorkResult>`, `OnResult` virtual 추가 |
| `SchedulingModule.cs` | RunAsync: result 캡처 → OnResult 호출 후 OnWorkEnded |
| `CustomerCycleSequenceSO.cs` | ExternallyCompleted 시 continue, step.OnResult 호출 |
| `ServeWorkSO.cs` | ExecuteAsync 결과 반환, NotifySuccess → OnResult 이동 |
| `CookWorkSO.cs` | ExecuteAsync 결과 반환, NotifyComplete → OnResult 이동 |
| `CashierWorkSO.cs` | `_completedSlot` 필드, OnResult 처리, null 버그 수정 |
| `CustomerAgent.cs` | OnPlayerInteract에서 OnFoodServed 직접 호출 제거 |
| 나머지 WorkSO 11개 | 반환형 변경 + catch → return 패턴 |

**변경 없음**: `WorkExecutionContext.cs`, `ISchedulable.cs`, `OrderTicket.cs`, 모든 SO 에셋, `OnWorkEnded` 시그니처

---

## 검증 방법

1. **정상 완료**: 스태프가 ServeWork 끝까지 실행 → `OnWorkEnded(true)`, 손님 FoodServed = true
2. **외부 완료 (Cook 중)**: CookWork 실행 중 ResolveWork → CookWorkSO.OnResult → NotifyComplete → 손님 FoodServed 경로 확인
3. **외부 완료 (Serve 중)**: ServeWork 실행 중 ResolveWork → ServeWorkSO.OnResult → NotifyComplete + OnFoodServed
4. **시퀀스 계속 진행**: CustomerCycle의 TakeSeat 중 ExternallyCompleted → 다음 스텝(WaitOrder 등)으로 계속 진행
5. **CashierWorkSO null slot**: DeQueue 전 취소 → OnResult에서 _completedSlot null → NotifyComplete 만 호출
6. **컴파일 오류 없음**
