# Work System Redesign — Design Spec

**Date:** 2026-05-14
**Branch:** BBJ
**Status:** Approved

---

## 배경

이전 스펙(`work-result-separation`, `player-intervention`)으로 `WorkResult`/`OnResult` 분리와 플레이어 개입 구조는 구현 완료.
이번 스펙은 그 위에 다음 4가지를 추가한다.

1. WorkSO 보일러플레이트 제거 (sealed ExecuteAsync + RunAsync 패턴)
2. SchedulingModule Pause / Resume
3. 플레이어 클릭 → 주문 즉시 처리 흐름
4. CookWorkSO 미니게임 pause 연동

---

## 섹션 1. WorkSO 보일러플레이트 제거

### 문제

모든 WorkSO 서브클래스가 동일한 try/catch 패턴을 반복한다.

```csharp
catch (OperationCanceledException)
{
    return ctx.WasExternallyCompleted
        ? WorkResult.ExternallyCompleted
        : WorkResult.Cancelled;
}
```

### 변경: ExecuteAsync sealed, RunAsync 신규

```csharp
public abstract class WorkSO : ScriptableObject
{
    public AgentRole RequiredRole;

    [SerializeField] private InterventionHandlerSO _playerHandler;
    public bool IsPlayerInteractable => _playerHandler != null;
    public InterventionHandlerSO PlayerHandler => _playerHandler;

    // 기반 클래스가 OCE 처리 — sealed로 오버라이드 금지
    public sealed async UniTask<WorkResult> ExecuteAsync(
        ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
    {
        try
        {
            return await RunAsync(executor, context, ctx);
        }
        catch (OperationCanceledException)
        {
            return ctx.WasExternallyCompleted
                ? WorkResult.ExternallyCompleted
                : WorkResult.Cancelled;
        }
    }

    // 서브클래스는 RunAsync만 구현 — guard clause + happy path만
    protected abstract UniTask<WorkResult> RunAsync(
        ModuleOwner executor, GameEvent context, WorkExecutionContext ctx);

    public virtual void OnResult(
        WorkResult result, ModuleOwner executor, GameEvent context) { }
}
```

### 서브클래스 변화

```csharp
// 변경 전 (~40줄)
public override async UniTask<WorkResult> ExecuteAsync(...) {
    try {
        await agent.MoveAsync(...);
        ctx.Token.ThrowIfCancellationRequested();
        return WorkResult.Completed;
    }
    catch (OperationCanceledException) {
        return ctx.WasExternallyCompleted ? WorkResult.ExternallyCompleted : WorkResult.Cancelled;
    }
}

// 변경 후 (~20줄)
protected override async UniTask<WorkResult> RunAsync(...) {
    await agent.MoveAsync(..., ctx.Token);
    await agent.DoWorkAsync(..., ctx.Token);
    return WorkResult.Completed;
    // try/catch 없음 — 기반 클래스가 처리
}
```

**영향 범위:** 모든 WorkSO 서브클래스 (`ExecuteAsync` → `RunAsync` 로 메서드명 변경)  
**예외:** `CustomerCycleSequenceSO` — `RunAsync`로 rename만, 내부 로직 유지

---

## 섹션 2. SchedulingModule Pause / Resume

### 목적

대사, 감정표현 등 게임적 표현을 위해 외부 시스템(FoodManager, 대화 시스템)이  
Work 실행을 일시정지/재개할 수 있어야 한다. SchedulingModule은 이유를 모른다.

### WorkExecutionContext 변경

```csharp
public sealed class WorkExecutionContext : IDisposable
{
    // 기존 필드 유지
    private UniTaskCompletionSource _pauseGate;

    public bool IsPaused => _pauseGate != null;

    public void Pause()
    {
        _pauseGate ??= new UniTaskCompletionSource();
    }

    public void Resume()
    {
        _pauseGate?.TrySetResult();
        _pauseGate = null;
    }

    // WorkSO 또는 시퀀스 러너가 pause 체크 지점에서 호출
    public UniTask WaitIfPausedAsync(CancellationToken ct = default)
    {
        if (_pauseGate == null) return UniTask.CompletedTask;
        return _pauseGate.Task.AttachExternalCancellation(ct);
    }

    // Dispose에 _pauseGate 해제 추가
}
```

### SchedulingModule 변경

```csharp
// 외부 시스템이 호출하는 공개 API
public void Pause()  => _execCtx?.Pause();
public void Resume() => _execCtx?.Resume();
```

### pause 체크 지점

**1. CustomerCycleSequenceSO — 스텝 사이 (기본 적용)**

```csharp
foreach (var step in _steps)
{
    if (step == null) continue;
    await ctx.WaitIfPausedAsync(ctx.Token);  // 추가 — 대사/감정 표현 대기
    stepResult = await step.ExecuteAsync(executor, context, ctx);
    step.OnResult(stepResult, executor, context);
    if (stepResult == WorkResult.Cancelled)           return WorkResult.Cancelled;
    if (stepResult == WorkResult.ExternallyCompleted) return WorkResult.Completed;
}
```

**2. 개별 WorkSO — 필요 시 직접 호출 (CookWorkSO 등)**

```csharp
ctx.Pause();
await ctx.WaitIfPausedAsync(ctx.Token);
```

외부 시스템은 `SchedulingModule.Pause()` / `SchedulingModule.Resume()`만 알면 된다.

---

## 섹션 3. 플레이어 클릭 → 주문 즉시 처리

### 흐름

```
[플레이어가 손님 클릭]
    ↓
customer.SchedulingModule.ResolveWork()   ← 클릭 핸들러
    ↓
WaitOrderWorkSO.RunAsync → OCE → ExternallyCompleted
    ↓
WaitOrderWorkSO.OnResult(ExternallyCompleted, customer, context)
    ├─ 1. 오고 있던 스태프 TakeOrder 취소
    ├─ 2. customer.PlaceOrder(seat)
    └─ 3. OrderManager.Instance.Register(ticket) → 요리 발행
```

### 스태프 추적 추가

**CustomerAgent — AssignedServer 추가**

```csharp
public ModuleOwner AssignedServer { get; private set; }
public void SetAssignedServer(ModuleOwner server) => AssignedServer = server;
```

**TakeOrderWorkSO.RunAsync — 시작/종료 시 등록**

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
{
    var ev       = context as TakeOrderEvent;
    var seat     = ev?.Seat;
    var seatMod  = seat?.GetModule<SeatModule>();
    var customer = seatMod?.AssignedAgent as CustomerAgent;

    customer?.SetAssignedServer(executor);  // 추가

    await (executor as IActionDispatcher).MoveAsync(
        seat.GetNearestPoint(executor.transform.position), ctx.Token);
    await (executor as IActionDispatcher).DoWorkAsync(seat, ctx.Token);

    var ticket = customer?.PlaceOrder(seat);
    if (ticket != null) OrderManager.Instance?.Register(ticket);

    customer?.SetAssignedServer(null);      // 추가
    return WorkResult.Completed;
}
```

**WaitOrderWorkSO.OnResult — ExternallyCompleted 처리 추가**

```csharp
public override void OnResult(
    WorkResult result, ModuleOwner executor, GameEvent context)
{
    if (result != WorkResult.ExternallyCompleted) return;

    var customer = executor as CustomerAgent;
    var seat     = customer?.AssignedSeat;

    // 1. 오고 있던 스태프 취소
    customer?.AssignedServer?.GetModule<SchedulingModule>()?.CancelWork();

    // 2. 주문 즉시 완료
    var ticket = customer?.PlaceOrder(seat);

    // 3. 요리 발행
    if (ticket != null) OrderManager.Instance?.Register(ticket);
}
```

**CustomerAgent.OnPlayerInteract() 제거**  
클릭 감지는 기존 `IPlayerInteractable` / `PlayerInterventionManager` 클릭 흐름을 재사용하거나,  
손님 오브젝트에 `customer.GetModule<SchedulingModule>().ResolveWork()`를 호출하는 클릭 핸들러로 대체.  
(`IsAwaitingOrder == true`인 경우에만 ResolveWork 허용하도록 가드 조건 추가)

---

## 섹션 4. CookWorkSO 미니게임 pause 연동

### 흐름

```
CookWorkSO.RunAsync
    → 주방 이동
    → CookingStartEvent 발행 (_cookingStartChannel)  ← FoodManager 수신
    → ctx.Pause() + await ctx.WaitIfPausedAsync()    ← 미니게임 중 일시정지
    → (FoodManager가 미니게임 완료 시 SchedulingModule.Resume() 호출)
    → RunAsync 반환 WorkResult.Completed
    → OnResult: OrderManager.NotifyComplete → ServeWork 발행
```

### CookWorkSO.RunAsync 변경

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
{
    var agent = executor as IActionDispatcher;
    var ev    = context as OrderWorkEvent;
    if (agent == null || ev == null) return WorkResult.Cancelled;
    if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

    var kitchen = _workplaceRegister
        .GetCandidates(executor.transform.position, _kitchenType)
        .FirstOrDefault(k => k.GetModule<OccupancyModule>()?.TryReserve(executor, null) == true);
    if (kitchen == null)
    {
        ev.OrderManager.NotifyReleased(ev.Ticket, executor);
        return WorkResult.Cancelled;
    }

    var foodContext = executor.GetModule<FoodContextModule>();
    try
    {
        await agent.MoveAsync(kitchen.GetNearestPoint(executor.transform.position), ctx.Token);
        ev.Ticket.TryStartProgress(executor);
        foodContext?.SetFood(ev.Ticket.Food);

        // DoWorkAsync 대신 미니게임 트리거 + pause
        _cookingStartChannel?.RaiseEvent(new CookingStartEvent
        {
            Ticket = ev.Ticket,
            Staff  = executor
        });
        ctx.Pause();
        await ctx.WaitIfPausedAsync(ctx.Token);  // FoodManager.Resume() 대기

        return WorkResult.Completed;
    }
    finally
    {
        foodContext?.ClearFood();
        kitchen.GetModule<OccupancyModule>()?.Release();
    }
    // try/catch 불필요 — 기반 클래스가 OCE 처리
}
```

### CookingStartEvent (신규 타입)

```csharp
public class CookingStartEvent : GameEvent
{
    public OrderTicket Ticket;
    public ModuleOwner Staff;
}
```

### FoodManager (미니게임 시스템)

```csharp
void OnCookingStart(CookingStartEvent ev)
{
    StartMiniGame(ev.Ticket.Food, onComplete: () =>
    {
        (ev.Staff as ModuleOwner)
            ?.GetModule<SchedulingModule>()
            ?.Resume();
    });
}
```

### OnResult — 변경 없음

```csharp
public override void OnResult(WorkResult result, ModuleOwner executor, GameEvent context)
{
    var ev = context as OrderWorkEvent;
    if (result != WorkResult.Cancelled)
        ev.OrderManager.NotifyComplete(ev.Ticket, executor);  // → ServeWork 발행
    else
        ev.OrderManager.NotifyReleased(ev.Ticket, executor);
}
```

서빙 대기 흐름(`WaitForFoodWorkSO` → `ServeWorkSO`)은 기존 유지.

---

## 변경 범위 요약

| 파일 | 변경 내용 |
|------|----------|
| `WorkSO.cs` | `ExecuteAsync` sealed, `RunAsync` abstract 신규 |
| `WorkExecutionContext.cs` | `Pause/Resume/WaitIfPausedAsync/IsPaused` 추가 |
| `SchedulingModule.cs` | `Pause()/Resume()` 공개 추가 |
| `CustomerCycleSequenceSO.cs` | 스텝 사이 `WaitIfPausedAsync` 추가, `RunAsync` rename |
| `CustomerAgent.cs` | `AssignedServer` 추가, `OnPlayerInteract` 제거 |
| `TakeOrderWorkSO.cs` | `RunAsync` rename, `SetAssignedServer` 호출 추가 |
| `WaitOrderWorkSO.cs` | `RunAsync` rename, `OnResult` ExternallyCompleted 처리 추가 |
| `CookWorkSO.cs` | `RunAsync` rename, `DoWorkAsync` → pause 방식으로 교체 |
| 나머지 WorkSO 7개 | `ExecuteAsync` → `RunAsync` rename만 |

**변경 없음:** `WorkResult.cs`, `OrderTicket.cs`, `OnResult` 시그니처, SO 에셋, 스태프 파이프라인

---

## 검증 방법 (Unity 에디터)

1. **손님 클릭 주문**: 손님이 WaitOrder 상태일 때 클릭 → 스태프 TakeOrder 취소 확인 → OrderManager에 티켓 등록 확인
2. **요리 미니게임**: CookWork 시작 → CookingStartEvent 발행 확인 → FoodManager.Resume() 호출 전까지 스태프 정지 → Resume 후 OnResult 호출 확인
3. **Pause/Resume**: 시퀀스 스텝 사이 Pause 호출 → 다음 스텝 진행 안 됨 → Resume → 진행 재개
4. **기존 스태프 흐름**: 플레이어 개입 없이 스태프가 전체 사이클 완료 — 기존 동작 유지
5. **컴파일 오류 없음**: RunAsync rename 이후 모든 WorkSO 컴파일 통과
