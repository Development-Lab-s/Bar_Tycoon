# Work Execution Context — Design Spec

**Date:** 2026-05-13  
**Branch:** BBJ  
**Status:** Approved

---

## 문제

현재 Work 시스템은 두 가지 문제를 가진다.

1. **`CompleteWork()` = 취소 = 실패**: `SchedulingModule.CompleteWork()`가 `_cts.Cancel()`만 호출하므로 WorkSO 입장에서 외부 완료와 강제 취소를 구분할 수 없다. 플레이어나 스케줄러가 Work를 "성공"으로 끝내고 싶어도 수단이 없다.

2. **완료 결과가 외부로 나오지 않음**: WorkSO 안에서 성공/실패 로직이 처리되고, SchedulingModule은 구분 없이 `OnWorkEnded`만 발생시킨다. 다음 Work를 결정할 외부 시스템이 결과를 볼 수 없다.

---

## 목표

- **외부 완료(성공)** 와 **강제 취소(실패)** 를 명확히 분리한다.
- 플레이어 Agent도 `ISchedulable`을 구현해 동일한 Work를 수행할 수 있다.
- 완료 결과(`bool` 성공 여부)를 `OnWorkEnded`로 외부에 노출해 다음 Work 분기를 가능하게 한다.
- WorkSO 내부 시퀀스(`CustomerCycleSequenceSO`)는 그대로 유지하되, 외부 완료 시 시퀀스 전체를 정상 종료한다.

---

## 핵심 설계: `WorkExecutionContext`

### 클래스

```csharp
// Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs
public sealed class WorkExecutionContext : IDisposable
{
    private readonly CancellationTokenSource _cancelCts;
    private readonly CancellationTokenSource _completeCts;
    private readonly CancellationTokenSource _linkedCts;

    public CancellationToken Token => _linkedCts.Token;
    public bool WasExternallyCompleted => _completeCts.IsCancellationRequested;

    public WorkExecutionContext()
    {
        _cancelCts  = new CancellationTokenSource();
        _completeCts = new CancellationTokenSource();
        _linkedCts  = CancellationTokenSource.CreateLinkedTokenSource(
                          _cancelCts.Token, _completeCts.Token);
    }

    internal void ForceComplete() => _completeCts.Cancel();
    internal void HardCancel()    => _cancelCts.Cancel();

    public void Dispose()
    {
        _cancelCts.Dispose();
        _completeCts.Dispose();
        _linkedCts.Dispose();
    }
}
```

### 신호 흐름

| 호출 | ctx.Token | ctx.WasExternallyCompleted | WorkSO 반응 |
|---|---|---|---|
| `ResolveWork()` | 취소됨 | `true` | catch when true → 성공 경로 |
| `CancelWork()` | 취소됨 | `false` | catch → 실패 경로, rethrow |

---

## WorkSO 시그니처 변경

```csharp
// 변경 전
UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct)

// 변경 후
UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
```

영향 범위: 모든 `WorkSO` 구현체 (ServeWorkSO, PayAtCounterWorkSO, TakeSeatWorkSO, WaitForFoodWorkSO, EatWorkSO, CookWorkSO, TakeOrderWorkSO, WaitOrderWorkSO, ExitWorkSO, CashierWorkSO, AgentActionWorkSO, CustomerCycleSequenceSO)

---

## WorkSO 구현 패턴

### 자체 완료 로직이 있는 WorkSO (ServeWorkSO, PayAtCounterWorkSO 등)

```csharp
public override async UniTask ExecuteAsync(
    ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
{
    try
    {
        // ... 이동 및 작업 await (ctx.Token 사용) ...
        NotifySuccess(...); // 정상 완료
    }
    catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
    {
        NotifySuccess(...); // 외부 완료도 성공으로 처리
    }
    catch (OperationCanceledException)
    {
        NotifyFailure(...); // 강제 취소
        throw;
    }
}
```

### 단순 대기 WorkSO (EatWorkSO, WaitForFoodWorkSO 등)

추가 catch 불필요. `OperationCanceledException`이 상위로 전파되면 `CustomerCycleSequenceSO`가 처리한다.

### CustomerCycleSequenceSO

```csharp
public override async UniTask ExecuteAsync(
    ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
{
    foreach (var step in _steps)
    {
        try
        {
            await step.ExecuteAsync(executor, context, ctx);
        }
        catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
        {
            return; // 현재 스텝 외부 완료 → 시퀀스 전체 정상 종료
        }

        if (ctx.WasExternallyCompleted) return; // 스텝이 내부에서 처리 후 반환한 경우
        ctx.Token.ThrowIfCancellationRequested(); // 강제 취소는 전파
    }
}
```

---

## SchedulingModule 변경

```csharp
public class SchedulingModule : MonoBehaviour, IModule, ISchedulable, ...
{
    private WorkExecutionContext _execCtx;

    // 기존 AssignWork — WorkExecutionContext 생성으로 교체
    public void AssignWork(WorkSO workSO, GameEvent context)
    {
        CancelWork();
        _execCtx = new WorkExecutionContext();
        RunAsync(workSO, context, _execCtx).Forget();
    }

    // 신규: 성공 완료 (플레이어/외부 개입)
    public void ResolveWork() => _execCtx?.ForceComplete();

    // 기존 CompleteWork → CancelWork로 rename (의미 명확화)
    public void CancelWork()
    {
        _execCtx?.HardCancel();
        _execCtx = null;
    }

    // OnWorkEnded에 성공 여부 추가
    public event Action<bool> OnWorkEnded; // true = 성공, false = 취소

    private async UniTaskVoid RunAsync(
        WorkSO workSO, GameEvent context, WorkExecutionContext ctx)
    {
        OnWorkStarted?.Invoke();
        bool success = false;
        try
        {
            await workSO.ExecuteAsync(_owner, context, ctx);
            success = true;
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (_execCtx == ctx) _execCtx = null;
            bool resolved = ctx.WasExternallyCompleted; // Dispose 전에 캡처
            ctx.Dispose();
            OnWorkEnded?.Invoke(success || resolved);
            ScheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());
        }
    }
}
```

---

## ISchedulable 인터페이스 변경

```csharp
public interface ISchedulable
{
    bool IsAvailableForWork { get; }
    AgentRole Role          { get; }

    event Action OnWorkStarted;
    event Action<bool> OnWorkEnded; // bool: 성공 여부 추가

    void AssignWork(WorkSO workSO, GameEvent context);
    void CancelWork();   // 구: CompleteWork
    void ResolveWork();  // 신규: 외부 성공 완료
}
```

---

## 플레이어 Agent 지원

플레이어 Agent는 `ISchedulable`을 구현하고 `ScheduleRegister`에 등록한다.  
`IActionDispatcher`와 `IPathMovement` 모듈이 붙어 있으면 기존 WorkSO를 그대로 받아 실행할 수 있다.  
별도 Work 구조 불필요 — 동일한 WorkSO, 동일한 `WorkExecutionContext` 패턴.

---

## 영향 범위 요약

| 파일 | 변경 내용 |
|---|---|
| `WorkExecutionContext.cs` | 신규 생성 |
| `WorkSO.cs` | 시그니처 변경 |
| `SchedulingModule.cs` | ResolveWork 추가, CancelWork rename, OnWorkEnded(bool) |
| `ISchedulable.cs` | ResolveWork 추가, OnWorkEnded(bool), CompleteWork→CancelWork |
| `CustomerCycleSequenceSO.cs` | 외부 완료 처리 추가 |
| `ServeWorkSO.cs` | ctx 패턴 적용 |
| `PayAtCounterWorkSO.cs` | ctx 패턴 적용 |
| `TakeSeatWorkSO.cs` | ctx 패턴 적용 |
| `EatWorkSO.cs` | 시그니처만 변경 |
| `WaitForFoodWorkSO.cs` | 시그니처만 변경 |
| `CookWorkSO.cs` | 시그니처만 변경 |
| `TakeOrderWorkSO.cs` | 시그니처만 변경 |
| `WaitOrderWorkSO.cs` | 시그니처만 변경 |
| `ExitWorkSO.cs` | 시그니처만 변경 |
| `CashierWorkSO.cs` | 시그니처만 변경 |
| `AgentActionWorkSO.cs` | 시그니처만 변경 |
| `ScheduleManager.cs` | ResolveWork 노출 여부 검토 |
| `CustomerAgent.cs` | `CompleteWork()` → `CancelWork()` (ResetItem 내부) |

---

## 검증 방법

1. **정상 완료**: 스태프가 Work를 끝까지 실행 → `OnWorkEnded(true)` 발생 확인
2. **외부 완료**: 런타임 중 `ResolveWork()` 호출 → WorkSO 성공 경로 실행, `OnWorkEnded(true)` 확인
3. **강제 취소**: `CancelWork()` 호출 → WorkSO 실패 경로(NotifyReleased), `OnWorkEnded(false)` 확인
4. **시퀀스 외부 완료**: CustomerCycleSequenceSO 실행 중 `ResolveWork()` → 현재 스텝만 성공 처리 후 시퀀스 종료
5. **컴파일 오류 없음**: 모든 WorkSO 시그니처 변경 후 Unity 컴파일 성공
