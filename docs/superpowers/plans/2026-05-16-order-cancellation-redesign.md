# Order Cancellation Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 취소 흐름을 단일 CancellationToken 전파로 통합하고, `OnResult`/`ExternallyCompleted` 시스템을 제거해 WorkSO가 자신의 실행만 책임지도록 재설계한다.

**Architecture:** `OrderTicket`이 `CancellationTokenSource`를 소유해 취소 시 연결된 모든 워커가 자동 중단된다. WorkSO는 `OnResult` 없이 `RunAsync` 안의 try/catch/finally에서 모든 케이스를 처리한다. 플레이어 개입은 별도 핸들러 컴포넌트가 직접 처리하며 WorkSO와 무관하다.

**Tech Stack:** Unity 2D, C#, UniTask (Cysharp), ScriptableObject Event Channel

---

## 파일 구조

| 파일 | 변경 |
|---|---|
| `Assets/00. Work/BBJ/02. Scripts/Order/OrderTicket.cs` | CTS 추가, `IsTerminal`, `Cancel()`/`Finish()` 수정 |
| `Assets/00. Work/BBJ/02. Scripts/Work/WorkResult.cs` | `ExternallyCompleted` 제거 |
| `Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs` | `_completeCts`, `ForceComplete()`, `WasExternallyCompleted` 제거 |
| `Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs` | `OnResult` virtual 제거, `ExecuteAsync` 단순화 |
| `Assets/00. Work/BBJ/02. Scripts/Schedule/ISchedulable.cs` | `ResolveWork()` 제거 |
| `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs` | `ResolveWork()`, `OnResult` 호출 제거 |
| `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs` | `OnResult` 제거, linked token + try/catch/finally |
| `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs` | `OnResult` 제거, linked token + try/catch/finally |
| `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs` | `OnResult` 제거, linked token + slot 보장 finally |
| `Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs` | `OnResult` 제거, RunAsync 인라인 타임아웃 처리 |
| `Assets/00. Work/BBJ/02. Scripts/Order/OrderManager.cs` | `IsTerminal` 체크로 교체 |
| `Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs` | `ResetItem` 이벤트 채널 경유 |
| `Assets/00. Work/BBJ/02. Scripts/Player/PlayerOrderHandler.cs` | **신규** — 플레이어 주문 개입 핸들러 |

---

## Task 1: OrderTicket — CancellationTokenSource 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Order/OrderTicket.cs`

- [ ] **Step 1: `OrderTicket`에 CTS와 관련 멤버 추가**

`OrderTicket.cs`의 기존 프로퍼티 블록 아래에 추가:

```csharp
// CancellationTokenSource — Cancel() 시 연결된 워커에 전파
private readonly CancellationTokenSource _cts = new();
public CancellationToken Token    => _cts.Token;
public bool              IsTerminal => State is OrderState.Done or OrderState.Cancelled;
```

상단 using 추가 (없으면):
```csharp
using System.Threading;
```

- [ ] **Step 2: `Cancel()` 수정 — CTS 발화 및 Dispose**

기존:
```csharp
internal void Cancel(CancelReason reason)
{
    CancellationReason = reason;
    ReservedBy = null;
    State = OrderState.Cancelled;
}
```

변경:
```csharp
internal void Cancel(CancelReason reason)
{
    CancellationReason = reason;
    ReservedBy = null;
    State = OrderState.Cancelled;
    _cts.Cancel();
    _cts.Dispose();
}
```

- [ ] **Step 3: `Finish()` 수정 — Dispose 추가**

기존:
```csharp
internal void Finish()
{
    ReservedBy = null;
    State = OrderState.Done;
}
```

변경:
```csharp
internal void Finish()
{
    ReservedBy = null;
    State = OrderState.Done;
    _cts.Dispose();
}
```

- [ ] **Step 4: Unity Editor 재진입 후 컴파일 에러 없음 확인**

- [ ] **Step 5: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Order/OrderTicket.cs"
git commit -m "feat: OrderTicket owns CancellationTokenSource for cancel propagation"
```

---

## Task 2: ExternallyCompleted 시스템 전체 제거

> 이 태스크는 여러 파일을 동시에 수정한다. 중간 상태는 컴파일 안 됨 — 모든 Step 완료 후 에디터 재진입.

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WorkResult.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Schedule/ISchedulable.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs`

- [ ] **Step 1: `WorkResult.cs` — `ExternallyCompleted` 제거**

변경 후 전체 파일:
```csharp
namespace BBJ.Work
{
    public enum WorkResult
    {
        Completed,
        Cancelled
    }
}
```

- [ ] **Step 2: `WorkExecutionContext.cs` — `_completeCts`, `ForceComplete()`, `WasExternallyCompleted` 제거**

변경 후 전체 파일:
```csharp
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace BBJ.Work
{
    public sealed class WorkExecutionContext : IDisposable
    {
        private readonly CancellationTokenSource _cancelCts;
        private UniTaskCompletionSource _pauseGate;

        public CancellationToken Token    => _cancelCts.Token;
        public bool              IsPaused => _pauseGate != null;

        public WorkExecutionContext()
        {
            _cancelCts = new CancellationTokenSource();
        }

        internal void HardCancel() => _cancelCts.Cancel();

        public void Pause()
        {
            if (_pauseGate == null)
                _pauseGate = new UniTaskCompletionSource();
        }

        public void Resume()
        {
            var gate = _pauseGate;
            _pauseGate = null;
            gate?.TrySetResult();
        }

        public UniTask WaitIfPausedAsync(CancellationToken waitCancelToken = default)
        {
            if (_pauseGate == null) return UniTask.CompletedTask;
            return _pauseGate.Task.AttachExternalCancellation(waitCancelToken);
        }

        public void Dispose()
        {
            _pauseGate?.TrySetCanceled();
            _pauseGate = null;
            _cancelCts.Dispose();
        }
    }
}
```

- [ ] **Step 3: `WorkSO.cs` — `OnResult` virtual 제거, `ExecuteAsync` 단순화**

변경 후 전체 파일:
```csharp
using BBJ.Staff;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    public abstract class WorkSO : ScriptableObject
    {
        public AgentRole RequiredRole;

        [SerializeField] protected WorkContextSO _ctx;

        public UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
            => RunAsync(executor, context, ctx);

        protected abstract UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx);
    }
}
```

- [ ] **Step 4: `ISchedulable.cs` — `ResolveWork()` 제거**

변경 후 전체 파일:
```csharp
using BBJ.Staff;
using BBJ.Work;
using Gamelib.EventSystem;
using System;

namespace BBJ.Schedule
{
    public interface ISchedulable
    {
        bool IsAvailableForWork { get; }
        AgentRole Role          { get; }

        event Action OnWorkStarted;
        event Action<bool> OnWorkEnded;
        void AssignWork(WorkSO workSO, GameEvent context);
        void CancelWork();
    }
}
```

- [ ] **Step 5: `SchedulingModule.cs` — `ResolveWork()` 제거, `OnResult` 호출 제거**

변경 후 전체 파일:
```csharp
using BBJ.Register;
using BBJ.Staff;
using BBJ.Work;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Schedule
{
    public class SchedulingModule : MonoBehaviour, IModule, ISchedulable, IAfterInitModule
    {
        [SerializeField] private ScheduleRegisterSO _scheduleRegister;
        [SerializeField] private EventChannelSO     _scheduleChannel;
        [field: SerializeField] public AgentRole Role { get; private set; }

        private ModuleOwner          _owner;
        private WorkExecutionContext _execCtx;

        public WorkSO    CurrentWork    { get; private set; }
        public GameEvent CurrentContext { get; private set; }

        public bool IsAvailableForWork => _execCtx == null;

        public event Action OnWorkStarted;
        public event Action<bool> OnWorkEnded;

        public void Initialize(ModuleOwner owner) => _owner = owner;
        public void AfterInit()
        {
            _scheduleRegister?.Register(this);
        }

        private void OnDisable()
        {
            _scheduleRegister?.Unregister(this);
            CancelWork();
        }

        public void AssignWork(WorkSO workSO, GameEvent context)
        {
            CancelWork();
            CurrentWork    = workSO;
            CurrentContext = context;
            _execCtx       = new WorkExecutionContext();
            RunAsync(workSO, context, _execCtx).Forget();
        }

        public void CancelWork()
        {
            _execCtx?.HardCancel();
            _execCtx = null;
        }

        public void Pause()  => _execCtx?.Pause();
        public void Resume() => _execCtx?.Resume();

        private async UniTaskVoid RunAsync(
            WorkSO workSO, GameEvent context, WorkExecutionContext ctx)
        {
            OnWorkStarted?.Invoke();
            WorkResult result = WorkResult.Cancelled;
            try
            {
                result = await workSO.ExecuteAsync(_owner, context, ctx);
            }
            catch (OperationCanceledException)
            {
                result = WorkResult.Cancelled;
            }
            finally
            {
                if (_execCtx == ctx) _execCtx = null;
                CurrentWork    = null;
                CurrentContext = null;
                ctx.Dispose();
                OnWorkEnded?.Invoke(result == WorkResult.Completed);
                _scheduleChannel?.RaiseEvent(new ScheduleTriggerEvent());
            }
        }
    }
}
```

- [ ] **Step 6: Unity Editor 재진입 후 컴파일 에러 없음 확인**

컴파일 에러가 나면 `ExternallyCompleted`나 `ResolveWork`를 아직 참조하는 파일을 찾아 제거:
```bash
# 프로젝트 Scripts 폴더에서 잔여 참조 검색
grep -r "ExternallyCompleted\|ResolveWork\|WasExternallyCompleted\|ForceComplete" "Assets/00. Work/BBJ/02. Scripts" --include="*.cs"
```

- [ ] **Step 7: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/WorkResult.cs" \
        "Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs" \
        "Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs" \
        "Assets/00. Work/BBJ/02. Scripts/Schedule/ISchedulable.cs" \
        "Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs"
git commit -m "refactor: remove ExternallyCompleted/OnResult system from WorkSO pipeline"
```

---

## Task 3: CookWorkSO — linked token + try/catch/finally

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs`

- [ ] **Step 1: `CookWorkSO` 전체 교체**

```csharp
using BBJ.Actions;
using BBJ.Modules;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Linq;
using System.Threading;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "CookWork", menuName = "Tycoon/Work/Cook")]
    public class CookWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var actions = executor.GetModule<AgentActionModule>();
            var ev      = context as OrderWorkEvent;
            if (actions == null || ev == null) return WorkResult.Cancelled;

            if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                ctx.Token, ev.Ticket.Token);

            var kitchen = _ctx.WorkplaceRegister
                .GetCandidates(executor.transform.position, _ctx.KitchenType)
                .FirstOrDefault(k => k.GetModule<OccupancyModule>()?.TryReserve(executor, null) == true);

            if (kitchen == null)
            {
                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ev.Ticket, executor));
                return WorkResult.Cancelled;
            }

            var foodContext = executor.GetModule<FoodContextModule>();
            try
            {
                await actions.Execute<MoveAction>(
                    a => a.ExecuteAsync(kitchen.GetNearestPoint(executor.transform.position), linked.Token));
                ev.Ticket.TryStartProgress(executor);
                foodContext?.SetFood(ev.Ticket.Food);

                if (_ctx.OrderChannel != null)
                {
                    _ctx.OrderChannel.RaiseEvent(new CookingStartEvent(ev.Ticket, executor));
                    ctx.Pause();
                    await ctx.WaitIfPausedAsync(linked.Token);
                }

                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ev.Ticket, executor));
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                if (!ev.Ticket.IsTerminal)
                    _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ev.Ticket, executor));
                return WorkResult.Cancelled;
            }
            finally
            {
                foodContext?.ClearFood();
                kitchen.GetModule<OccupancyModule>()?.Release();
            }
        }
    }
}
```

- [ ] **Step 2: Unity Editor 재진입 후 컴파일 에러 없음 확인**

- [ ] **Step 3: Play Mode 확인 — 정상 요리 흐름**

손님 스폰 → 서버가 주문 접수 → 요리사가 이동 → 요리 완료 → `OrderNotifyCompleteEvent` 발생 → 서빙 단계로 진행됨.

- [ ] **Step 4: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs"
git commit -m "refactor: CookWorkSO uses linked ticket token, removes OnResult"
```

---

## Task 4: ServeWorkSO — linked token + try/catch/finally

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs`

- [ ] **Step 1: `ServeWorkSO` 전체 교체**

```csharp
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ServeWork", menuName = "Tycoon/Work/Serve")]
    public class ServeWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var actions = executor.GetModule<AgentActionModule>();
            var ev      = context as OrderWorkEvent;
            if (actions == null || ev == null) return WorkResult.Cancelled;

            if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

            var serveStation = _ctx.WorkplaceRegister?.GetFirst(_ctx.ServeStationType);
            if (serveStation == null) return WorkResult.Cancelled;

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                ctx.Token, ev.Ticket.Token);

            try
            {
                Vector3 from = executor.transform.position;
                await actions.Execute<MoveAction>(
                    a => a.ExecuteAsync(serveStation.GetNearestPoint(from), linked.Token));
                ev.Ticket.TryStartProgress(executor);

                await actions.Execute<MoveAction>(
                    a => a.ExecuteAsync(ev.Ticket.Seat.GetNearestPoint(from), linked.Token));
                await actions.Execute<WorkAction>(
                    a => a.ExecuteAsync(ev.Ticket.Seat, linked.Token));

                var customer = ev.Ticket.Seat.GetModule<SeatModule>()?.AssignedAgent as CustomerAgent;
                customer?.OnFoodServed();

                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ev.Ticket, executor));
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                if (!ev.Ticket.IsTerminal)
                    _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ev.Ticket, executor));
                return WorkResult.Cancelled;
            }
        }
    }
}
```

- [ ] **Step 2: Unity Editor 재진입 후 컴파일 에러 없음 확인**

- [ ] **Step 3: Play Mode 확인 — 서빙 흐름**

요리 완료 → 서버가 서빙 스테이션 이동 → 손님 자리로 이동 → 서빙 완료 → `OnFoodServed` 호출 → 결제 단계로 진행됨.

- [ ] **Step 4: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs"
git commit -m "refactor: ServeWorkSO uses linked ticket token, removes OnResult"
```

---

## Task 5: CashierWorkSO — linked token + slot 보장

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs`

> **주의:** 기존 `WasExternallyCompleted` 로직은 "slot이 dequeue됐지만 NotifyProcessed 전에 중단"되는 케이스를 다뤘다. 새 설계에서는 finally에서 무조건 NotifyProcessed를 호출해 동일하게 보장한다.

- [ ] **Step 1: `CashierWorkSO` 전체 교체**

```csharp
using BBJ.Modules;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Actions;
using BBJ.WorkplaceSystem;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CashierWork", menuName = "Tycoon/Work/Cashier")]
    public class CashierWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var actions = executor.GetModule<AgentActionModule>();
            var ev      = context as OrderWorkEvent;
            if (actions == null || ev == null) return WorkResult.Cancelled;

            if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

            var counter = _ctx.WorkplaceRegister?.GetFirst(_ctx.CounterType);
            if (counter == null)
            {
                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ev.Ticket, executor));
                return WorkResult.Cancelled;
            }

            var queue = counter.GetModule<WorkplaceQueueModule>();
            if (queue == null)
            {
                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ev.Ticket, executor));
                return WorkResult.Cancelled;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                ctx.Token, ev.Ticket.Token);

            OccupationSlot? slot      = null;
            bool            processed = false;
            try
            {
                await actions.Execute<MoveAction>(
                    a => a.ExecuteAsync(counter.GetNearestPoint(executor.transform.position), linked.Token));
                ev.Ticket.TryStartProgress(executor);
                await actions.Execute<WaitAction>(
                    a => a.ExecuteAsync(() => queue.HasWaiting, linked.Token));

                slot = queue.Dequeue();
                if (slot == null)
                {
                    _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ev.Ticket, executor));
                    return WorkResult.Cancelled;
                }

                await actions.Execute<WorkAction>(
                    a => a.ExecuteAsync(counter, linked.Token));

                slot.Value.NotifyProcessed();
                processed = true;

                _ctx.OrderChannel?.RaiseEvent(new OrderNotifyCompleteEvent(ev.Ticket, executor));
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                if (!ev.Ticket.IsTerminal)
                    _ctx.OrderChannel?.RaiseEvent(new OrderNotifyReleasedEvent(ev.Ticket, executor));
                return WorkResult.Cancelled;
            }
            finally
            {
                // slot이 dequeue됐지만 NotifyProcessed 전에 중단된 경우 보장
                if (!processed && slot.HasValue)
                    slot.Value.NotifyProcessed();
            }
        }
    }
}
```

- [ ] **Step 2: Unity Editor 재진입 후 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs"
git commit -m "refactor: CashierWorkSO uses linked ticket token, removes OnResult"
```

---

## Task 6: WaitOrderWorkSO — OnResult 제거, 타임아웃 인라인

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs`

> **주의:** `WaitOrderWorkSO`는 손님이 주문을 기다리는 단계다. 이 시점에 `OrderTicket`이 아직 존재하지 않으므로 linked token을 쓰지 않고 `ctx.Token`만 사용한다.
>
> 기존 `OnResult(ExternallyCompleted)`에서 하던 "서버 취소 + 주문 등록"은 Task 9에서 만들 `PlayerOrderHandler`가 직접 처리한다. 여기서는 제거만 한다.

- [ ] **Step 1: `WaitOrderWorkSO` 전체 교체**

```csharp
using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Schedule;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Actions;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "WaitOrderWork", menuName = "Tycoon/Work/WaitOrder")]
    public class WaitOrderWorkSO : WorkSO
    {
        [SerializeField] private float _patienceLimit = 60f;

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var actions  = executor.GetModule<AgentActionModule>();
            var seat     = customer?.AssignedSeat;
            if (customer == null || actions == null || seat == null) return WorkResult.Cancelled;

            try
            {
                customer.SetAwaitingOrder(true);
                _ctx.DispatchTable?.Dispatch(OrderWorkPhase.ReadyForServer, new TakeOrderEvent(seat));
                float deadline = Time.time + _patienceLimit;

                await actions.Execute<WaitAction>(a => a.ExecuteAsync(
                    () => customer.OrderPlaced || Time.time >= deadline, ctx.Token));

                // 타임아웃: 담당 서버 작업 취소 (티켓 없음)
                if (!customer.OrderPlaced)
                    customer.AssignedServer?.GetModule<ISchedulable>()?.CancelWork();

                return WorkResult.Completed;
            }
            finally
            {
                customer.SetAwaitingOrder(false);
            }
        }
    }
}
```

- [ ] **Step 2: Unity Editor 재진입 후 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs"
git commit -m "refactor: WaitOrderWorkSO removes OnResult, inlines timeout handling"
```

---

## Task 7: OrderManager — IsTerminal 체크

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Order/OrderManager.cs`

- [ ] **Step 1: `CancelOrder` 조건 교체**

기존:
```csharp
public void CancelOrder(OrderTicket ticket, CancelReason reason)
{
    if (ticket.State is OrderState.Done or OrderState.Cancelled) return;
    ...
}
```

변경:
```csharp
public void CancelOrder(OrderTicket ticket, CancelReason reason)
{
    if (ticket.IsTerminal) return;
    ...
}
```

- [ ] **Step 2: Unity Editor 재진입 후 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Order/OrderManager.cs"
git commit -m "refactor: OrderManager uses IsTerminal guard in CancelOrder"
```

---

## Task 8: CustomerAgent.ResetItem — OrderManager 경유 취소

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs`

- [ ] **Step 1: `ResetItem` 수정**

기존 `ResetItem`:
```csharp
public override void ResetItem()
{
    GetModule<SchedulingModule>()?.CancelWork();
    AssignedSeat    = null;
    AssignedServer  = null;
    ActiveTicket    = null;
    ...
}
```

변경:
```csharp
public override void ResetItem()
{
    // 활성 티켓이 있으면 OrderManager 흐름을 통해 취소 → CTS 전파로 진행 중인 워커도 중단
    if (ActiveTicket != null && !ActiveTicket.IsTerminal)
        _orderChannel?.RaiseEvent(new OrderCancelRequestEvent(ActiveTicket, CancelReason.CustomerLeft));

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

필요한 using 확인 (`BBJ.EventSystem` 네임스페이스):
```csharp
using BBJ.EventSystem;
```

- [ ] **Step 2: Unity Editor 재진입 후 컴파일 에러 없음 확인**

- [ ] **Step 3: Play Mode 확인 — 손님 강제 퇴장**

요리 진행 중 손님을 풀로 반환(CustomerManager에서 강제 `ResetItem` 호출) → `OrderCancelRequestEvent` 발행 → `OrderManager.CancelOrder` 실행 → `ticket.Cancel()` → CTS 발화 → 요리사 작업 OperationCanceledException → 레지스트리에서 티켓 제거됨.

- [ ] **Step 4: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs"
git commit -m "fix: CustomerAgent.ResetItem cancels active ticket through OrderManager"
```

---

## Task 9: PlayerOrderHandler — 플레이어 주문 개입 핸들러 신규 생성

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Player/PlayerOrderHandler.cs`

> 기존 플레이어 개입 핸들러는 `335c6dd`에서 제거됐다. 이 태스크는 새로운 구조에 맞게 재작성한다.
>
> 이 컴포넌트는 손님 클릭 이벤트를 수신해 두 가지를 처리한다:
> 1. 담당 서버의 진행 중인 `TakeOrderWork` 취소
> 2. 주문 직접 등록

- [ ] **Step 1: `Assets/00. Work/BBJ/02. Scripts/Player/` 폴더 생성 확인**

폴더가 없으면 Unity Project 창에서 생성하거나 파일 저장 시 자동 생성.

- [ ] **Step 2: `PlayerOrderHandler.cs` 생성**

```csharp
using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Schedule;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Player
{
    public class PlayerOrderHandler : MonoBehaviour
    {
        [SerializeField] private EventChannelSO _orderChannel;

        // 외부(Input Handler, UI 등)에서 손님 클릭 시 호출
        public void OnCustomerClicked(CustomerAgent customer)
        {
            if (customer == null) return;
            if (!customer.IsReadyForOrder) return;
            if (customer.OrderPlaced) return;

            var seat = customer.AssignedSeat;
            if (seat == null) return;

            // 1. 이동 중인 서버 작업 취소
            customer.AssignedServer?.GetModule<ISchedulable>()?.CancelWork();

            // 2. 주문 직접 등록
            var ticket = customer.PlaceOrder(seat);
            if (ticket != null)
                _orderChannel?.RaiseEvent(new OrderTicketRegisterEvent(ticket));
        }
    }
}
```

- [ ] **Step 3: Unity Editor 재진입 후 컴파일 에러 없음 확인**

- [ ] **Step 4: `PlayerOrderHandler` 컴포넌트를 씬의 적절한 GameObject에 추가하고 `_orderChannel` Inspector 연결**

기존 플레이어 입력 시스템에서 `PlayerOrderHandler.OnCustomerClicked(customer)`를 호출하도록 연결한다.

- [ ] **Step 5: Play Mode 확인 — 플레이어 개입 흐름**

손님 스폰 → 서버가 손님을 향해 이동 중 → 플레이어가 손님 클릭 → 서버 작업 취소되고 멈춤 → 주문 등록됨 → 요리 단계 진행됨.

- [ ] **Step 6: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Player/PlayerOrderHandler.cs"
git commit -m "feat: add PlayerOrderHandler for direct order intervention"
```

---

## 최종 검증 (전체 흐름)

- [ ] **시나리오 1 — 정상 흐름:** 손님 스폰 → 서버 주문 접수 → 요리 → 서빙 → 결제 → Done
- [ ] **시나리오 2 — 플레이어 개입:** 서버 이동 중 손님 클릭 → 주문 직등록 → 서버 멈춤 → 요리 진행
- [ ] **시나리오 3 — 손님 퇴장:** 요리 중 손님 ResetItem → CTS 전파 → 요리사 중단 → 레지스트리 정리
- [ ] **시나리오 4 — 씬 언로드:** 에디터에서 Play Stop → 진행 중 티켓 모두 Cancelled
- [ ] **시나리오 5 — 타임아웃:** WaitOrder 60초 경과 → 서버 작업 취소 → 손님 이탈 흐름
