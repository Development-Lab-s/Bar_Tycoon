# Work Execution Context Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `CompleteWork()`(취소)와 `ResolveWork()`(외부 성공 완료)를 분리해 플레이어·스케줄러가 Work를 성공으로 끝낼 수 있게 하고, 완료 결과를 외부로 노출한다.

**Architecture:** `WorkExecutionContext`가 두 개의 CancellationTokenSource(하드 취소/외부 완료)를 감싸고, WorkSO는 하나의 링크드 토큰으로 대기하다가 `catch when ctx.WasExternallyCompleted`로 성공/실패 경로를 분기한다. `CustomerCycleSequenceSO`는 외부 완료 시 시퀀스를 정상 종료한다.

**Tech Stack:** Unity 2D, C#, UniTask (Cysharp), ScriptableObject 기반 WorkSO 패턴

---

## 파일 맵

| 동작 | 파일 |
|---|---|
| 신규 | `Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs` |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs` |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/CustomerCycleSequenceSO.cs` |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs` |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs` |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs` |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Work/TakeSeatWorkSO.cs` |
| 수정(시그니처만) | `Assets/00. Work/BBJ/02. Scripts/Work/EatWorkSO.cs` |
| 수정(시그니처만) | `Assets/00. Work/BBJ/02. Scripts/Work/WaitForFoodWorkSO.cs` |
| 수정(시그니처만) | `Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs` |
| 수정(시그니처만) | `Assets/00. Work/BBJ/02. Scripts/Work/ExitWorkSO.cs` |
| 수정(시그니처만) | `Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs` |
| 수정(시그니처만) | `Assets/00. Work/BBJ/02. Scripts/Work/AgentActionWorkSO.cs` |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Schedule/ISchedulable.cs` |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs` |
| 수정 | `Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs` |

> **커밋 전략 (컴파일 안정):**
> - Commit A: WorkExecutionContext 신규 파일만
> - Commit B: WorkSO 시그니처 + 모든 WorkSO 구현체 (한 번에)
> - Commit C: ISchedulable + SchedulingModule
> - Commit D: CustomerAgent

---

## Task 1: WorkExecutionContext 생성

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs`

- [ ] **Step 1: 파일 생성**

```csharp
using System;
using System.Threading;

namespace BBJ.Work
{
    public sealed class WorkExecutionContext : IDisposable
    {
        private readonly CancellationTokenSource _cancelCts;
        private readonly CancellationTokenSource _completeCts;
        private readonly CancellationTokenSource _linkedCts;

        public CancellationToken Token => _linkedCts.Token;
        public bool WasExternallyCompleted => _completeCts.IsCancellationRequested;

        public WorkExecutionContext()
        {
            _cancelCts   = new CancellationTokenSource();
            _completeCts = new CancellationTokenSource();
            _linkedCts   = CancellationTokenSource.CreateLinkedTokenSource(
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
}
```

- [ ] **Step 2: Unity 컴파일 확인**

Unity Editor 콘솔에 에러 없음 확인. 이 단계는 기존 코드를 전혀 건드리지 않으므로 에러가 없어야 한다.

- [ ] **Step 3: Commit A**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs" \
        "Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs.meta"
git commit -m "feat: WorkExecutionContext 추가 - 외부 완료/강제 취소 토큰 분리"
```

---

## Task 2: WorkSO 시그니처 변경 + 모든 구현체 업데이트

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CustomerCycleSequenceSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/TakeSeatWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/EatWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WaitForFoodWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/ExitWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/AgentActionWorkSO.cs`

> **주의:** 이 Task는 WorkSO 시그니처 변경과 모든 구현체를 **한 커밋에** 처리한다. 중간 상태로 커밋하면 컴파일 에러가 남는다.

- [ ] **Step 1: WorkSO.cs — abstract 시그니처 변경**

`CancellationToken ct` → `WorkExecutionContext ctx` 로 교체. `using System.Threading` 제거, `using BBJ.Work` (자신이므로 불필요) 확인.

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
        public abstract UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, WorkExecutionContext ctx);
    }
}
```

- [ ] **Step 2: CustomerCycleSequenceSO.cs — 시퀀스 외부 완료 처리**

```csharp
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CustomerCycleSequence", menuName = "Tycoon/Work/CustomerCycleSequence")]
    public class CustomerCycleSequenceSO : WorkSO
    {
        [SerializeField] private WorkSO[] _steps;

        public override async UniTask ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            if (_steps == null) return;

            foreach (var step in _steps)
            {
                if (step == null) continue;

                try
                {
                    await step.ExecuteAsync(executor, context, ctx);
                }
                catch (System.OperationCanceledException) when (ctx.WasExternallyCompleted)
                {
                    return; // 현재 스텝 외부 완료 → 시퀀스 정상 종료
                }

                if (ctx.WasExternallyCompleted) return; // 스텝이 내부에서 처리 후 반환한 경우
                ctx.Token.ThrowIfCancellationRequested(); // 강제 취소는 전파
            }
        }
    }
}
```

- [ ] **Step 3: ServeWorkSO.cs — 완료 로직 포함 WorkSO**

```csharp
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ServeWork", menuName = "Tycoon/Work/Serve")]
    public class ServeWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;
        [SerializeField] private WorkplaceTypeSO _serveStationTypeSO;

        public override async UniTask ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;

            if (!ev.Ticket.TryReserve(executor)) return;

            var serveStation = _workplaceRegister?.GetFirst(_serveStationTypeSO);
            if (serveStation == null)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                return;
            }

            Vector3 from = executor.transform.position;

            try
            {
                Vector3 serveStationPos = serveStation.GetNearestPoint(from);
                await agent.MoveAsync(serveStationPos, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                ev.Ticket.TryStartProgress(executor);
                Vector3 seatPos = ev.Ticket.Seat.GetNearestPoint(from);

                await agent.MoveAsync(seatPos, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                await agent.DoWorkAsync(ev.Ticket.Seat, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                NotifySuccess(ev, executor);
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                NotifySuccess(ev, executor);
            }
            catch (OperationCanceledException)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                throw;
            }
        }

        private void NotifySuccess(OrderWorkEvent ev, ModuleOwner executor)
        {
            ev.OrderManager.NotifyComplete(ev.Ticket, executor);
            var seatModule = ev.Ticket.Seat.GetModule<SeatModule>();
            var customer   = seatModule?.AssignedAgent as CustomerAgent;
            customer?.OnFoodServed();
        }
    }
}
```

- [ ] **Step 4: CookWorkSO.cs — 완료 로직 포함 WorkSO**

```csharp
using BBJ.Actions;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using System.Linq;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "CookWork", menuName = "Tycoon/Work/Cook")]
    public class CookWorkSO : WorkSO
    {
        [UnityEngine.SerializeField] private WorkplaceTypeSO     _kitchenType;
        [UnityEngine.SerializeField] private WorkplaceRegisterSO _workplaceRegister;

        public override async UniTask ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;
            if (agent == null || ev == null) return;

            var actor = executor;
            if (!ev.Ticket.TryReserve(actor)) return;

            var kitchen = _workplaceRegister
                .GetCandidates(executor.transform.position, _kitchenType)
                .FirstOrDefault(k => k.GetModule<OccupancyModule>()?.TryReserve(executor, null) == true);

            if (kitchen == null)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, actor);
                return;
            }

            var foodContext = executor.GetModule<FoodContextModule>();
            try
            {
                await agent.MoveAsync(kitchen.GetNearestPoint(executor.transform.position), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                ev.Ticket.TryStartProgress(actor);
                foodContext?.SetFood(ev.Ticket.Food);

                await agent.DoWorkAsync(kitchen, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                ev.OrderManager.NotifyComplete(ev.Ticket, actor);
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                ev.OrderManager.NotifyComplete(ev.Ticket, actor);
            }
            catch (OperationCanceledException)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, actor);
                throw;
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

- [ ] **Step 5: CashierWorkSO.cs — 완료 로직 포함 WorkSO**

```csharp
using BBJ.Actions;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CashierWork", menuName = "Tycoon/Work/Cashier")]
    public class CashierWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceTypeSO     _counterType;
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;

        public override async UniTask ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;
            if (agent == null || ev == null) return;

            if (!ev.Ticket.TryReserve(executor)) return;

            var counter = _workplaceRegister?.GetFirst(_counterType);
            if (counter == null) { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return; }

            var queue = counter.GetModule<WorkplaceQueueModule>();
            if (queue == null)  { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return; }

            OccupationSlot? slot = null;
            try
            {
                await agent.MoveAsync(counter.GetNearestPoint(executor.transform.position), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                ev.Ticket.TryStartProgress(executor);

                await agent.WaitUntilAsync(() => queue.HasWaiting, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                slot = queue.Dequeue();
                if (slot == null) { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return; }

                await agent.DoWorkAsync(counter, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                slot.Value.NotifyProcessed();
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                slot?.Value.NotifyProcessed();
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
            }
            catch (OperationCanceledException)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                throw;
            }
        }
    }
}
```

- [ ] **Step 6: TakeSeatWorkSO.cs — 완료 로직 포함 WorkSO**

```csharp
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Movement;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using System.Linq;
using UnityEngine;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "TakeSeatWork", menuName = "Tycoon/Work/TakeSeat")]
    public class TakeSeatWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO _seatType;

        public override async UniTask ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return;

            var seat = _register
                .GetCandidates(executor.transform.position, _seatType)
                .FirstOrDefault(s => {
                    var occ = s.GetModule<OccupancyModule>();
                    return occ != null && !occ.IsOccupied && occ.TryReserve(executor, null);
                });

            if (seat == null) return;

            customer.AssignedSeat = seat;
            seat.GetModule<OccupancyModule>()?.Occupy(executor);

            var dest       = seat.GetNearestPoint(executor.transform.position);
            var seatModule = seat.GetModule<SeatModule>();
            seatModule?.AssignCustomer(executor);

            try
            {
                await agent.MoveAsync(dest, ctx.Token);
                seatModule?.Seat(executor);
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                seatModule?.Seat(executor); // 이동 없이 즉시 착석
            }
            catch (OperationCanceledException)
            {
                seat.GetModule<OccupancyModule>()?.Release();
                customer.AssignedSeat = null;
                throw;
            }
        }
    }
}
```

- [ ] **Step 7: 시그니처만 변경하는 WorkSO 6개**

각 파일에서 메서드 시그니처의 `CancellationToken ct` → `WorkExecutionContext ctx` 로 교체하고, 본문 내 `ct` → `ctx.Token` 으로 교체한다. `using System.Threading` 제거.

**EatWorkSO.cs:**
```csharp
using BBJ.Actions;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "EatWork", menuName = "Tycoon/Work/Eat")]
    public class EatWorkSO : WorkSO
    {
        [SerializeField] private float _eatDuration = 8f;

        public override UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            if (agent != null)
                return agent.WaitAsync(_eatDuration, ctx.Token);
            return UniTask.Delay(TimeSpan.FromSeconds(_eatDuration), cancellationToken: ctx.Token);
        }
    }
}
```

**WaitForFoodWorkSO.cs:**
```csharp
using BBJ.Actions;
using BBJ.Customer;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "WaitForFoodWork", menuName = "Tycoon/Work/WaitForFood")]
    public class WaitForFoodWorkSO : WorkSO
    {
        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return;
            await agent.WaitUntilAsync(() => customer.FoodServed, ctx.Token);
        }
    }
}
```

**WaitOrderWorkSO.cs:**
```csharp
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.Schedule;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "WaitOrderWork", menuName = "Tycoon/Work/WaitOrder")]
    public class WaitOrderWorkSO : WorkSO
    {
        [SerializeField] private WorkDispatchTableSO _dispatchTable;
        [SerializeField] private float               _patienceLimit = 60f;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            var seat     = customer?.AssignedSeat;
            if (customer == null || agent == null || seat == null) return;

            customer.SetAwaitingOrder(true);
            _dispatchTable?.Dispatch(OrderWorkPhase.ReadyForServer, new TakeOrderEvent(seat), ScheduleManager.Instance);
            await agent.WaitUntilAsync(() => customer.OrderPlaced, ctx.Token, _patienceLimit);
            customer.SetAwaitingOrder(false);
        }
    }
}
```

**ExitWorkSO.cs:**
```csharp
using BBJ.Actions;
using BBJ.Customer;
using BBJ.EventSystem;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ExitWork", menuName = "Tycoon/Work/Exit")]
    public class ExitWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO     _exitType;
        [SerializeField] private EventChannelSO      _customerChannel;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            if (customer == null) return;

            var seat  = customer.AssignedSeat;
            var agent = executor as IActionDispatcher;

            if (seat != null)
            {
                seat.GetModule<SeatModule>()?.ClearCustomer();
                seat.GetModule<OccupancyModule>()?.Release();
                customer.AssignedSeat = null;

                var exits = _register?.GetAll(_exitType);
                if (exits != null && exits.Count > 0 && agent != null)
                    await agent.MoveAsync(exits[0].GetNearestPoint(executor.transform.position), ctx.Token);
            }

            _customerChannel?.RaiseEvent(new CustomerLeftEvent { Customer = customer });
        }
    }
}
```

**TakeOrderWorkSO.cs:**
```csharp
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "TakeOrderWork", menuName = "Tycoon/Work/TakeOrder")]
    public class TakeOrderWorkSO : WorkSO
    {
        public override async UniTask ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as TakeOrderEvent;
            if (agent == null || ev == null) return;

            var seat = ev.Seat;
            await agent.MoveAsync(seat.GetNearestPoint(executor.transform.position), ctx.Token);
            ctx.Token.ThrowIfCancellationRequested();

            await agent.DoWorkAsync(seat, ctx.Token);
            ctx.Token.ThrowIfCancellationRequested();

            var seatModule = seat.GetModule<SeatModule>();
            var customer   = seatModule?.AssignedAgent as CustomerAgent;
            var ticket     = customer?.PlaceOrder(seat);

            if (ticket != null)
                OrderManager.Instance?.Register(ticket);
        }
    }
}
```

**AgentActionWorkSO.cs:**
```csharp
using BBJ.Actions;
using BBJ.Staff.FSM;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    public class AgentActionWorkSO : WorkSO
    {
        [SerializeField] private TycoonAgentAction action;

        public override async UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            if (agent == null) return;
            await agent.ExecuteStateAsync(action, context, ctx.Token);
        }
    }
}
```

- [ ] **Step 8: Unity 컴파일 확인**

Unity Editor 콘솔에 에러 없음 확인. 모든 WorkSO 구현체가 업데이트되었으므로 `WorkSO.ExecuteAsync` 관련 에러가 남아있으면 빠진 파일이 있는 것이다.

- [ ] **Step 9: Commit B**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/"
git commit -m "refactor: WorkSO 시그니처 CancellationToken→WorkExecutionContext 전환"
```

---

## Task 3: ISchedulable 인터페이스 + SchedulingModule 업데이트

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Schedule/ISchedulable.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs`

> 두 파일을 한 커밋에 처리한다. ISchedulable 변경 시 SchedulingModule도 즉시 맞춰야 컴파일된다.

- [ ] **Step 1: ISchedulable.cs 업데이트**

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
        event Action<bool> OnWorkEnded; // bool: true=성공, false=취소

        void AssignWork(WorkSO workSO, GameEvent context);
        void CancelWork();   // 강제 취소 (구 CompleteWork)
        void ResolveWork();  // 외부 성공 완료 (신규)
    }

    public interface IScheduleTriggerSource
    {
        Gamelib.EventSystem.EventChannelSO ScheduleTriggerChannel { get; }
    }
}
```

- [ ] **Step 2: SchedulingModule.cs 전체 교체**

```csharp
using BBJ.Register;
using BBJ.Schedule;
using BBJ.Staff;
using BBJ.Work;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Schedule
{
    public class SchedulingModule : MonoBehaviour, IModule, ISchedulable, IScheduleTriggerSource, IAfterInitModule
    {
        [SerializeField] private ScheduleRegisterSO _scheduleRegister;
        [field: SerializeField] public EventChannelSO ScheduleTriggerChannel { get; private set; }
        [field: SerializeField] public AgentRole Role { get; private set; }

        private ModuleOwner          _owner;
        private WorkExecutionContext _execCtx;

        public bool IsAvailableForWork => _execCtx == null;

        public event Action OnWorkStarted;
        public event Action<bool> OnWorkEnded;

        public void Initialize(ModuleOwner owner) => _owner = owner;

        private void OnDisable()
        {
            _scheduleRegister?.Unregister(this);
            CancelWork();
        }

        public void AfterInit() => _scheduleRegister?.Register(this);

        public void AssignWork(WorkSO workSO, GameEvent context)
        {
            CancelWork();
            _execCtx = new WorkExecutionContext();
            RunAsync(workSO, context, _execCtx).Forget();
        }

        public void ResolveWork() => _execCtx?.ForceComplete();

        public void CancelWork()
        {
            _execCtx?.HardCancel();
            _execCtx = null;
        }

        private async UniTaskVoid RunAsync(WorkSO workSO, GameEvent context, WorkExecutionContext ctx)
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
                bool resolved = ctx.WasExternallyCompleted;
                ctx.Dispose();
                OnWorkEnded?.Invoke(success || resolved);
                ScheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());
            }
        }
    }
}
```

- [ ] **Step 3: Unity 컴파일 확인**

Unity Editor 콘솔에 에러 없음 확인.

- [ ] **Step 4: Commit C**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Schedule/ISchedulable.cs" \
        "Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs"
git commit -m "feat: ISchedulable에 ResolveWork 추가, OnWorkEnded(bool) 결과 노출"
```

---

## Task 4: CustomerAgent 수정

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs`

- [ ] **Step 1: ResetItem 내 CompleteWork → CancelWork 변경**

`CustomerAgent.cs` 파일에서 `ResetItem` 메서드를 찾아 아래 한 줄만 변경한다.

```csharp
// 변경 전
GetModule<SchedulingModule>()?.CompleteWork();

// 변경 후
GetModule<SchedulingModule>()?.CancelWork();
```

- [ ] **Step 2: Unity 컴파일 확인**

Unity Editor 콘솔에 에러 없음 확인.

- [ ] **Step 3: Commit D**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs"
git commit -m "fix: CustomerAgent.ResetItem — CompleteWork → CancelWork"
```

---

## Task 5: Unity 에디터 검증

- [ ] **Step 1: 정상 완료 확인**

플레이 모드 진입 → 손님 스폰 → 손님이 전체 사이클(착석→주문→음식 대기→음식 받기→식사→계산→퇴장) 완료하는지 확인. 콘솔 에러 없음.

- [ ] **Step 2: 외부 완료(ResolveWork) 확인**

임시 테스트 코드를 씬의 빈 MonoBehaviour에 추가해 확인한다.

```csharp
// 테스트용 임시 코드 (확인 후 삭제)
void Update()
{
    if (Input.GetKeyDown(KeyCode.F1))
    {
        foreach (var sm in FindObjectsOfType<BBJ.Schedule.SchedulingModule>())
        {
            if (!sm.IsAvailableForWork)
            {
                sm.ResolveWork();
                Debug.Log($"[Test] ResolveWork called on {sm.name}");
                break;
            }
        }
    }
}
```

플레이 모드에서 F1 키 → 현재 작업 중인 스태프/손님의 Work가 즉시 성공으로 처리되고 다음 단계로 진행하는지 확인. `OnWorkEnded(true)` 발생 여부 로그로 확인 가능.

- [ ] **Step 3: 강제 취소(CancelWork) 확인**

위 임시 코드에 F2 키 추가:

```csharp
if (Input.GetKeyDown(KeyCode.F2))
{
    foreach (var sm in FindObjectsOfType<BBJ.Schedule.SchedulingModule>())
    {
        if (!sm.IsAvailableForWork)
        {
            sm.CancelWork();
            Debug.Log($"[Test] CancelWork called on {sm.name}");
            break;
        }
    }
}
```

F2 키 → 작업이 중단되고 티켓이 NotifyReleased 처리(다시 큐에 들어가거나 해제)되는지 확인. `OnWorkEnded(false)` 발생.

- [ ] **Step 4: 시퀀스 외부 완료 확인**

손님이 `WaitForFoodWorkSO` 대기 중일 때 F1 → 현재 대기 스텝만 종료되고 시퀀스 전체가 정상 종료(Eat, 계산, 퇴장 없이 종료)되는지 확인.

- [ ] **Step 5: 임시 테스트 코드 제거**

확인 완료 후 테스트용 MonoBehaviour 제거.
