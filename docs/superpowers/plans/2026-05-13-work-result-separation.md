# Work Result Separation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** WorkSO.ExecuteAsync가 실행(이동·대기)만 담당하고 도메인 상태 변경(NotifyComplete, OnFoodServed 등)은 OnResult 오버라이드로 분리해, 취소·외부완료·정상완료 경로 각각에서 상태가 올바르게 처리되도록 한다.

**Architecture:** `WorkResult` enum(Completed/ExternallyCompleted/Cancelled)을 도입하고 WorkSO에 `virtual void OnResult(WorkResult, ModuleOwner, GameEvent)` 를 추가한다. SchedulingModule.RunAsync가 ExecuteAsync 결과를 캡처해 OnResult를 호출하고, CustomerCycleSequenceSO는 각 스텝의 OnResult를 순서대로 호출한 후 결과에 따라 시퀀스를 계속하거나 종료한다.

**Tech Stack:** Unity 2022+, C# 9, Cysharp/UniTask, ScriptableObject 기반 WorkSO 패턴

---

## 파일 구조

| 파일 | 작업 |
|---|---|
| `Assets/00. Work/BBJ/02. Scripts/Work/WorkResult.cs` | **신규** |
| `Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs` | 반환형 변경 + OnResult 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Work/CustomerCycleSequenceSO.cs` | 결과 처리 로직 변경 |
| `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs` | OnResult 추가, ExecuteAsync 정리 |
| `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs` | OnResult 추가, ExecuteAsync 정리 |
| `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs` | slot 처리 변경 + OnResult |
| `Assets/00. Work/BBJ/02. Scripts/Work/TakeSeatWorkSO.cs` | 반환형 변경 |
| `Assets/00. Work/BBJ/02. Scripts/Work/PayAtCounterWorkSO.cs` | 반환형 변경 |
| `Assets/00. Work/BBJ/02. Scripts/Work/EatWorkSO.cs` | 반환형 변경 |
| `Assets/00. Work/BBJ/02. Scripts/Work/WaitForFoodWorkSO.cs` | 반환형 변경 |
| `Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs` | 반환형 변경 |
| `Assets/00. Work/BBJ/02. Scripts/Work/ExitWorkSO.cs` | 반환형 변경 |
| `Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs` | 반환형 변경 |
| `Assets/00. Work/BBJ/02. Scripts/Work/AgentActionWorkSO.cs` | 반환형 변경 |
| `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs` | RunAsync 결과 처리 |
| `Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs` | OnPlayerInteract 수정 |

---

## Task 1: WorkResult 열거형 생성

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Work/WorkResult.cs`

- [ ] **Step 1: WorkResult.cs 생성**

```csharp
namespace BBJ.Work
{
    public enum WorkResult
    {
        Completed,
        ExternallyCompleted,
        Cancelled
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity Editor로 포커스를 이동해 컴파일이 완료되면 Console에 오류가 없는지 확인한다.

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/WorkResult.cs" "Assets/00. Work/BBJ/02. Scripts/Work/WorkResult.cs.meta"
git commit -m "feat: add WorkResult enum for work execution outcome"
```

---

## Task 2: WorkSO 베이스 + 전체 구현체 반환형 변경

> **주의**: WorkSO.cs를 먼저 변경하면 모든 서브클래스에서 컴파일 오류가 발생한다.
> Step 1~10을 모두 완료한 후 Step 11에서 한 번만 컴파일·커밋한다.

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs`
- Modify: 아래 열거된 13개 WorkSO 파일

### 중요 규칙
- 이 Task에서는 기존 상태 변경 코드(NotifyComplete, OnFoodServed 등)를 **삭제하지 않는다**.
- 반환형만 `UniTask<WorkResult>`로 바꾸고, 각 경로 끝에 `return WorkResult.XXX`를 추가한다.
- `throw;` (재throw)는 모두 `return WorkResult.Cancelled`로 교체한다.

- [ ] **Step 1: WorkSO.cs 변경**

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

        public abstract UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx);

        public virtual void OnResult(
            WorkResult result, ModuleOwner executor, GameEvent context) { }
    }
}
```

- [ ] **Step 2: EatWorkSO.cs 변경**

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

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            try
            {
                if (agent != null)
                    await agent.WaitAsync(_eatDuration, ctx.Token);
                else
                    await UniTask.Delay(TimeSpan.FromSeconds(_eatDuration), cancellationToken: ctx.Token);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
```

- [ ] **Step 3: WaitForFoodWorkSO.cs 변경**

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
        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return WorkResult.Cancelled;
            try
            {
                await agent.WaitUntilAsync(() => customer.FoodServed, ctx.Token);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
```

- [ ] **Step 4: WaitOrderWorkSO.cs 변경**

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

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            var seat     = customer?.AssignedSeat;
            if (customer == null || agent == null || seat == null) return WorkResult.Cancelled;

            try
            {
                customer.SetAwaitingOrder(true);
                _dispatchTable?.Dispatch(OrderWorkPhase.ReadyForServer, new TakeOrderEvent(seat), ScheduleManager.Instance);
                await agent.WaitUntilAsync(() => customer.OrderPlaced, ctx.Token, _patienceLimit);
                customer.SetAwaitingOrder(false);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                customer.SetAwaitingOrder(false);
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
```

- [ ] **Step 5: ExitWorkSO.cs 변경**

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

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            if (customer == null) return WorkResult.Cancelled;

            var seat  = customer.AssignedSeat;
            var agent = executor as IActionDispatcher;

            try
            {
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
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
```

- [ ] **Step 6: AgentActionWorkSO.cs 변경**

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

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            if (agent == null) return WorkResult.Cancelled;
            try
            {
                await agent.ExecuteStateAsync(action, context, ctx.Token);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
```

- [ ] **Step 7: TakeOrderWorkSO.cs 변경**

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
        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as TakeOrderEvent;
            if (agent == null || ev == null) return WorkResult.Cancelled;

            var seat = ev.Seat;
            try
            {
                await agent.MoveAsync(seat.GetNearestPoint(executor.transform.position), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                await agent.DoWorkAsync(seat, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                var seatModule = seat.GetModule<SeatModule>();
                var customer   = seatModule?.AssignedAgent as CustomerAgent;
                var ticket     = customer?.PlaceOrder(seat);

                if (ticket != null)
                    OrderManager.Instance?.Register(ticket);

                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
```

- [ ] **Step 8: TakeSeatWorkSO.cs 변경**

```csharp
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Linq;
using UnityEngine;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "TakeSeatWork", menuName = "Tycoon/Work/TakeSeat")]
    public class TakeSeatWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO     _seatType;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return WorkResult.Cancelled;

            var seat = _register
                .GetCandidates(executor.transform.position, _seatType)
                .FirstOrDefault(s => {
                    var occ = s.GetModule<OccupancyModule>();
                    return occ != null && !occ.IsOccupied && occ.TryReserve(executor, null);
                });

            if (seat == null) return WorkResult.Cancelled;

            customer.AssignedSeat = seat;
            seat.GetModule<OccupancyModule>()?.Occupy(executor);

            var dest       = seat.GetNearestPoint(executor.transform.position);
            var seatModule = seat.GetModule<SeatModule>();
            seatModule?.AssignCustomer(executor);

            try
            {
                await agent.MoveAsync(dest, ctx.Token);
                seatModule?.Seat(executor);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                seatModule?.Seat(executor);
                return WorkResult.ExternallyCompleted;
            }
            catch (OperationCanceledException)
            {
                seat.GetModule<OccupancyModule>()?.Release();
                customer.AssignedSeat = null;
                return WorkResult.Cancelled;
            }
        }
    }
}
```

- [ ] **Step 9: PayAtCounterWorkSO.cs 변경**

```csharp
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "PayAtCounterWork", menuName = "Tycoon/Work/PayAtCounter")]
    public class PayAtCounterWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _register;
        [SerializeField] private WorkplaceTypeSO     _counterType;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return WorkResult.Cancelled;

            try
            {
                customer.AssignedSeat.GetModule<SeatModule>().UnSeat();
                var counter = _register?.GetFirst(_counterType);
                await agent.MoveAsync(counter.GetNearestPoint(executor.transform.position), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                var payQueue = counter.GetModule<WorkplaceQueueModule>();
                if (payQueue == null) return WorkResult.Cancelled;

                bool paid = false;
                var slot = new OccupationSlot(
                    executor.transform,
                    pos => agent.MoveAsync(pos, ctx.Token).Forget(),
                    () => { customer.OnPaymentDone(); paid = true; });
                payQueue.Enqueue(slot);

                await agent.WaitUntilAsync(() => paid, ctx.Token);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return ctx.WasExternallyCompleted
                    ? WorkResult.ExternallyCompleted
                    : WorkResult.Cancelled;
            }
        }
    }
}
```

- [ ] **Step 10: ServeWorkSO.cs 변경 (상태 변경 코드 유지, 반환형만 추가)**

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
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ServeWork", menuName = "Tycoon/Work/Serve")]
    public class ServeWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;
        [SerializeField] private WorkplaceTypeSO     _serveStationTypeSO;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;

            if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

            var serveStation = _workplaceRegister?.GetFirst(_serveStationTypeSO);
            if (serveStation == null)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                return WorkResult.Cancelled;
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
                return WorkResult.Completed;
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                NotifySuccess(ev, executor);
                return WorkResult.ExternallyCompleted;
            }
            catch (OperationCanceledException)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                return WorkResult.Cancelled;
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

- [ ] **Step 11: CookWorkSO.cs 변경 (상태 변경 코드 유지, 반환형만 추가)**

```csharp
using BBJ.Actions;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Linq;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "CookWork", menuName = "Tycoon/Work/Cook")]
    public class CookWorkSO : WorkSO
    {
        [UnityEngine.SerializeField] private WorkplaceTypeSO     _kitchenType;
        [UnityEngine.SerializeField] private WorkplaceRegisterSO _workplaceRegister;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;
            if (agent == null || ev == null) return WorkResult.Cancelled;

            var actor = executor;
            if (!ev.Ticket.TryReserve(actor)) return WorkResult.Cancelled;

            var kitchen = _workplaceRegister
                .GetCandidates(executor.transform.position, _kitchenType)
                .FirstOrDefault(k => k.GetModule<OccupancyModule>()?.TryReserve(executor, null) == true);

            if (kitchen == null)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, actor);
                return WorkResult.Cancelled;
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
                return WorkResult.Completed;
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                ev.OrderManager.NotifyComplete(ev.Ticket, actor);
                return WorkResult.ExternallyCompleted;
            }
            catch (OperationCanceledException)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, actor);
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

- [ ] **Step 12: CashierWorkSO.cs 변경 (상태 변경 코드 유지, 반환형만 추가)**

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

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;
            if (agent == null || ev == null) return WorkResult.Cancelled;

            if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

            var counter = _workplaceRegister?.GetFirst(_counterType);
            if (counter == null) { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return WorkResult.Cancelled; }

            var queue = counter.GetModule<WorkplaceQueueModule>();
            if (queue == null)  { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return WorkResult.Cancelled; }

            OccupationSlot? slot = null;
            try
            {
                await agent.MoveAsync(counter.GetNearestPoint(executor.transform.position), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                ev.Ticket.TryStartProgress(executor);

                await agent.WaitUntilAsync(() => queue.HasWaiting, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                slot = queue.Dequeue();
                if (slot == null) { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return WorkResult.Cancelled; }

                await agent.DoWorkAsync(counter, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                slot.Value.NotifyProcessed();
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
                return WorkResult.Completed;
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                slot?.NotifyProcessed();
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
                return WorkResult.ExternallyCompleted;
            }
            catch (OperationCanceledException)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
                return WorkResult.Cancelled;
            }
        }
    }
}
```

- [ ] **Step 13: CustomerCycleSequenceSO.cs 변경**

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

                if (stepResult == WorkResult.Cancelled)           return WorkResult.Cancelled;
                if (stepResult == WorkResult.ExternallyCompleted) return WorkResult.Completed;
                // Completed → 다음 스텝으로
            }

            return WorkResult.Completed;
        }
    }
}
```

- [ ] **Step 14: 컴파일 확인**

Unity Editor로 포커스를 이동해 컴파일을 기다린다. Console에 오류가 없어야 한다.

Expected: Console에 컴파일 오류 0개.

- [ ] **Step 15: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/EatWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/WaitForFoodWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/ExitWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/AgentActionWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/TakeSeatWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/PayAtCounterWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs" ^
        "Assets/00. Work/BBJ/02. Scripts/Work/CustomerCycleSequenceSO.cs"
git commit -m "refactor: change WorkSO.ExecuteAsync to return WorkResult"
```

---

## Task 3: SchedulingModule RunAsync 변경

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs`

- [ ] **Step 1: RunAsync 메서드 교체**

`SchedulingModule.cs` 의 `RunAsync` 메서드 전체를 아래로 교체한다.

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

using 상단에 `using BBJ.Work;` 가 없다면 추가한다 (이미 있음).

- [ ] **Step 2: 컴파일 확인**

Unity Editor 컴파일 후 Console에 오류가 없어야 한다.

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs"
git commit -m "refactor: SchedulingModule calls workSO.OnResult with WorkResult"
```

---

## Task 4: ServeWorkSO — ExecuteAsync 정리 + OnResult 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs`

- [ ] **Step 1: ServeWorkSO.cs 전체 교체**

```csharp
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ServeWork", menuName = "Tycoon/Work/Serve")]
    public class ServeWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;
        [SerializeField] private WorkplaceTypeSO     _serveStationTypeSO;

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
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity Editor 컴파일 후 Console에 오류가 없어야 한다.

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs"
git commit -m "refactor: move ServeWorkSO state mutations to OnResult"
```

---

## Task 5: CookWorkSO — ExecuteAsync 정리 + OnResult 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs`

- [ ] **Step 1: CookWorkSO.cs 전체 교체**

```csharp
using BBJ.Actions;
using BBJ.Modules;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Linq;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "CookWork", menuName = "Tycoon/Work/Cook")]
    public class CookWorkSO : WorkSO
    {
        [UnityEngine.SerializeField] private WorkplaceTypeSO     _kitchenType;
        [UnityEngine.SerializeField] private WorkplaceRegisterSO _workplaceRegister;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;
            if (agent == null || ev == null) return WorkResult.Cancelled;

            var actor = executor;
            if (!ev.Ticket.TryReserve(actor)) return WorkResult.Cancelled;

            var kitchen = _workplaceRegister
                .GetCandidates(executor.transform.position, _kitchenType)
                .FirstOrDefault(k => k.GetModule<OccupancyModule>()?.TryReserve(executor, null) == true);

            if (kitchen == null)
            {
                ev.OrderManager.NotifyReleased(ev.Ticket, actor);
                return WorkResult.Cancelled;
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
    }
}
```

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs"
git commit -m "refactor: move CookWorkSO state mutations to OnResult"
```

---

## Task 6: CashierWorkSO — slot 처리 변경 + OnResult 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs`

- [ ] **Step 1: CashierWorkSO.cs 전체 교체**

`slot.NotifyProcessed()`는 try 블록 마지막(정상 완료 직전)에 직접 호출한다.
SO 공유 인스턴스 문제를 피하기 위해 인스턴스 필드를 쓰지 않는다.

```csharp
using BBJ.Actions;
using BBJ.Order;
using BBJ.Register;
using BBJ.WorkplaceSystem;
using BBJ.WorkplaceSystem.Modules;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CashierWork", menuName = "Tycoon/Work/Cashier")]
    public class CashierWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceTypeSO     _counterType;
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;

        public override async UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;
            if (agent == null || ev == null) return WorkResult.Cancelled;

            if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

            var counter = _workplaceRegister?.GetFirst(_counterType);
            if (counter == null) { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return WorkResult.Cancelled; }

            var queue = counter.GetModule<WorkplaceQueueModule>();
            if (queue == null)  { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return WorkResult.Cancelled; }

            OccupationSlot? slot = null;
            try
            {
                await agent.MoveAsync(counter.GetNearestPoint(executor.transform.position), ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                ev.Ticket.TryStartProgress(executor);

                await agent.WaitUntilAsync(() => queue.HasWaiting, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                slot = queue.Dequeue();
                if (slot == null) { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return WorkResult.Cancelled; }

                await agent.DoWorkAsync(counter, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                slot.Value.NotifyProcessed();
                return WorkResult.Completed;
            }
            catch (OperationCanceledException)
            {
                // ExternallyCompleted이고 slot이 이미 dequeue됐다면 고객에게 결제 완료 전달
                if (ctx.WasExternallyCompleted)
                    slot?.NotifyProcessed();
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
    }
}
```

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs"
git commit -m "refactor: CashierWorkSO slot handling and OnResult separation"
```

---

## Task 7: CustomerAgent.OnPlayerInteract 수정

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs`

- [ ] **Step 1: OnPlayerInteract 메서드 교체**

`CustomerAgent.cs` 의 `OnPlayerInteract` 메서드를 아래로 교체한다.

```csharp
public void OnPlayerInteract()
{
    ActiveTicket?.ReservedBy?.GetModule<SchedulingModule>()?.ResolveWork();
}
```

제거하는 코드:
```csharp
// 아래 두 줄 삭제
if (OrderPlaced && !FoodServed) OnFoodServed();
```

이유: `ResolveWork()` → `ServeWorkSO.OnResult` → `OnFoodServed()` 경로가 이미 처리하므로 직접 호출이 이중 실행을 유발한다.

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs"
git commit -m "fix: remove duplicate OnFoodServed call in OnPlayerInteract"
```

---

## Task 8: Unity 에디터 검증

**검증 시나리오 — 각 항목을 Play Mode에서 확인한다.**

- [ ] **시나리오 1: 정상 완료**

스태프가 ServeWork를 끝까지 실행한다.
- 손님 FoodServed = true, OrderTicket 완료 상태 확인
- Console에 오류 없음

- [ ] **시나리오 2: CookWork 외부 완료**

스태프가 CookWork(주방 이동 중)인 상태에서 손님을 클릭한다.
- CookWorkSO.OnResult → OrderManager.NotifyComplete 호출 확인
- 이어서 ServeWork가 디스패치되어 스태프가 서빙 동선을 밟는지 확인

- [ ] **시나리오 3: ServeWork 외부 완료**

스태프가 ServeWork(서브 스테이션 이동 중)인 상태에서 손님을 클릭한다.
- ServeWorkSO.OnResult → NotifyComplete + OnFoodServed 확인
- 손님이 WaitForFood 해제 후 Eat → PayAtCounter → Exit 진행하는지 확인

- [ ] **시나리오 4: CustomerCycleSequenceSO 연속 진행**

TakeSeat 스텝이 ExternallyCompleted 되어도 손님이 WaitOrder로 이동하는지 확인.
(현재 코드에서는 발생하기 어렵지만, ctx.WasExternallyCompleted 조건 삽입 후에도 시퀀스가 계속되는지 로그로 확인)

- [ ] **시나리오 5: CashierWork null slot 경로**

카운터 대기 고객이 없는 상태에서 CashierWork 취소.
- NotifyComplete가 호출되지 않고 NotifyReleased가 호출되는지 Console 로그로 확인.
