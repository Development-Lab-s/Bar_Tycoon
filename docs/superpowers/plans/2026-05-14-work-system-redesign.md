# Work System Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** WorkSO 보일러플레이트 제거(RunAsync 패턴), SchedulingModule Pause/Resume, 플레이어 클릭 주문 흐름, CookWorkSO 미니게임 pause 연동.

**Architecture:** WorkSO 기반 클래스가 `ExecuteAsync`를 `sealed`로 처리해 OCE try/catch를 흡수한다. 서브클래스는 `RunAsync`만 구현하고 try/finally로 리소스 정리를 담당한다. `WorkExecutionContext`에 pause gate를 추가하고 `SchedulingModule`이 외부 시스템에 `Pause()/Resume()`을 노출한다.

**Tech Stack:** Unity 2D, C#, UniTask (Cysharp), ScriptableObject-based architecture

---

## 파일 맵

| 파일 | 변경 유형 |
|------|----------|
| `Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs` | `ExecuteAsync` sealed, `RunAsync` abstract 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Work/EatWorkSO.cs` | `ExecuteAsync` → `RunAsync`, try/catch 제거 |
| `Assets/00. Work/BBJ/02. Scripts/Work/AgentActionWorkSO.cs` | 동일 |
| `Assets/00. Work/BBJ/02. Scripts/Work/WaitForFoodWorkSO.cs` | 동일 |
| `Assets/00. Work/BBJ/02. Scripts/Work/ExitWorkSO.cs` | 동일 |
| `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs` | 동일 |
| `Assets/00. Work/BBJ/02. Scripts/Work/PayAtCounterWorkSO.cs` | 동일 + 불필요한 ThrowIfCancellationRequested 제거 |
| `Assets/00. Work/BBJ/02. Scripts/Work/TakeSeatWorkSO.cs` | 동일 + 두 catch → try/finally 재구성 |
| `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs` | 동일 + slot 처리 finally 재구성 |
| `Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs` | 동일 + finally 재구성, `OnResult` 추가 (Task 4) |
| `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs` | 동일 + DoWorkAsync → pause 교체 (Task 5) |
| `Assets/00. Work/BBJ/02. Scripts/Work/CustomerCycleSequenceSO.cs` | rename + `WaitIfPausedAsync` 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs` | Pause/Resume/WaitIfPausedAsync 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Work/WorkEvents.cs` | `CookingStartEvent` 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs` | `Pause()/Resume()` 공개 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs` | `AssignedServer` 추가, `OnPlayerInteract` 제거 |
| `Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs` | rename + `SetAssignedServer` 호출 추가 |

---

## Task 1: WorkSO base + 전체 RunAsync 리네임

이 Task는 한 번의 컴파일 단위로 완료해야 한다.  
WorkSO.cs를 sealed로 변경하면 모든 서브클래스의 `override ExecuteAsync`가 컴파일 오류가 나므로  
WorkSO.cs 변경과 모든 서브클래스 rename을 같은 커밋에 완료한다.

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/EatWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/AgentActionWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WaitForFoodWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/ExitWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/PayAtCounterWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/TakeSeatWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CustomerCycleSequenceSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs`

---

- [ ] **Step 1-1: WorkSO.cs — ExecuteAsync sealed, RunAsync abstract 추가**

`Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs` 전체 내용:

```csharp
using BBJ.Staff;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using System;

namespace BBJ.Work
{
    public abstract class WorkSO : ScriptableObject
    {
        public AgentRole RequiredRole;

        [SerializeField] private InterventionHandlerSO _playerHandler;
        public bool IsPlayerInteractable => _playerHandler != null;
        public InterventionHandlerSO PlayerHandler => _playerHandler;

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

        protected abstract UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx);

        public virtual void OnResult(
            WorkResult result, ModuleOwner executor, GameEvent context) { }
    }
}
```

---

- [ ] **Step 1-2: EatWorkSO — RunAsync, try/catch 제거**

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

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            if (agent != null)
                await agent.WaitAsync(_eatDuration, ctx.Token);
            else
                await UniTask.Delay(TimeSpan.FromSeconds(_eatDuration), cancellationToken: ctx.Token);
            return WorkResult.Completed;
        }
    }
}
```

---

- [ ] **Step 1-3: AgentActionWorkSO — RunAsync, try/catch 제거**

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

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            if (agent == null) return WorkResult.Cancelled;
            await agent.ExecuteStateAsync(action, context, ctx.Token);
            return WorkResult.Completed;
        }
    }
}
```

---

- [ ] **Step 1-4: WaitForFoodWorkSO — RunAsync, try/catch 제거**

```csharp
using BBJ.Actions;
using BBJ.Customer;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [UnityEngine.CreateAssetMenu(fileName = "WaitForFoodWork", menuName = "Tycoon/Work/WaitForFood")]
    public class WaitForFoodWorkSO : WorkSO
    {
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return WorkResult.Cancelled;
            await agent.WaitUntilAsync(() => customer.FoodServed, ctx.Token);
            return WorkResult.Completed;
        }
    }
}
```

---

- [ ] **Step 1-5: ExitWorkSO — RunAsync, try/catch 제거**

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

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            if (customer == null) return WorkResult.Cancelled;

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
            return WorkResult.Completed;
        }
    }
}
```

---

- [ ] **Step 1-6: ServeWorkSO — RunAsync, try/catch 제거**

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

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as OrderWorkEvent;
            if (agent == null || ev == null) return WorkResult.Cancelled;

            if (!ev.Ticket.TryReserve(executor)) return WorkResult.Cancelled;

            var serveStation = _workplaceRegister?.GetFirst(_serveStationTypeSO);
            if (serveStation == null) return WorkResult.Cancelled;

            Vector3 from = executor.transform.position;
            await agent.MoveAsync(serveStation.GetNearestPoint(from), ctx.Token);
            ev.Ticket.TryStartProgress(executor);

            await agent.MoveAsync(ev.Ticket.Seat.GetNearestPoint(from), ctx.Token);
            await agent.DoWorkAsync(ev.Ticket.Seat, ctx.Token);
            return WorkResult.Completed;
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

---

- [ ] **Step 1-7: PayAtCounterWorkSO — RunAsync, try/catch 및 불필요한 ThrowIfCancellationRequested 제거**

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

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            if (customer == null || agent == null) return WorkResult.Cancelled;

            customer.AssignedSeat?.GetModule<SeatModule>()?.UnSeat();
            var counter = _register?.GetFirst(_counterType);
            if (counter == null) return WorkResult.Cancelled;

            await agent.MoveAsync(counter.GetNearestPoint(executor.transform.position), ctx.Token);

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
    }
}
```

---

- [ ] **Step 1-8: TakeSeatWorkSO — RunAsync, 두 catch → try/finally 재구성**

ExternallyCompleted 시: 자리에 착석(Seat)만 처리.  
Cancelled 시: 좌석 예약 해제 + AssignedSeat 초기화.  
`finally`에서 `ctx.WasExternallyCompleted`로 분기.

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

        protected override async UniTask<WorkResult> RunAsync(
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

            bool seated = false;
            try
            {
                await agent.MoveAsync(dest, ctx.Token);
                seatModule?.Seat(executor);
                seated = true;
                return WorkResult.Completed;
            }
            finally
            {
                if (!seated)
                {
                    if (ctx.WasExternallyCompleted)
                        seatModule?.Seat(executor);
                    else
                    {
                        seat.GetModule<OccupancyModule>()?.Release();
                        customer.AssignedSeat = null;
                    }
                }
            }
        }
    }
}
```

---

- [ ] **Step 1-9: CashierWorkSO — RunAsync, slot 처리 finally 재구성**

`slot.NotifyProcessed()`는 happy path와 ExternallyCompleted 시 모두 호출해야 한다.  
`NotifyComplete/NotifyReleased`는 `OnResult`가 담당 (기존 유지).

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

        protected override async UniTask<WorkResult> RunAsync(
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

            OccupationSlot? slot     = null;
            bool            notified = false;
            try
            {
                await agent.MoveAsync(counter.GetNearestPoint(executor.transform.position), ctx.Token);
                ev.Ticket.TryStartProgress(executor);
                await agent.WaitUntilAsync(() => queue.HasWaiting, ctx.Token);

                slot = queue.Dequeue();
                if (slot == null) { ev.OrderManager.NotifyReleased(ev.Ticket, executor); return WorkResult.Cancelled; }

                await agent.DoWorkAsync(counter, ctx.Token);
                slot.Value.NotifyProcessed();
                notified = true;
                return WorkResult.Completed;
            }
            finally
            {
                if (!notified && ctx.WasExternallyCompleted && slot.HasValue)
                    slot.Value.NotifyProcessed();
            }
        }

        public override void OnResult(WorkResult result, ModuleOwner executor, GameEvent context)
        {
            var ev = context as OrderWorkEvent;
            if (ev == null) return;
            if (result != WorkResult.Cancelled)
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
            else
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
        }
    }
}
```

---

- [ ] **Step 1-10: WaitOrderWorkSO — RunAsync, catch → finally 재구성**

`OnResult`(ExternallyCompleted 처리)는 Task 4에서 추가한다.

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

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            var seat     = customer?.AssignedSeat;
            if (customer == null || agent == null || seat == null) return WorkResult.Cancelled;

            customer.SetAwaitingOrder(true);
            _dispatchTable?.Dispatch(OrderWorkPhase.ReadyForServer, new TakeOrderEvent(seat), ScheduleManager.Instance);
            try
            {
                await agent.WaitUntilAsync(() => customer.OrderPlaced, ctx.Token, _patienceLimit);
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

---

- [ ] **Step 1-11: CookWorkSO — RunAsync, try/catch 제거 (finally 유지)**

DoWorkAsync는 Task 5에서 교체한다. 이 단계에서는 rename만.

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
                await agent.DoWorkAsync(kitchen, ctx.Token);
                return WorkResult.Completed;
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
            if (ev == null) return;
            if (result != WorkResult.Cancelled)
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
            else
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
        }
    }
}
```

---

- [ ] **Step 1-12: TakeOrderWorkSO — RunAsync (SetAssignedServer는 Task 4에서 추가)**

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
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent = executor as IActionDispatcher;
            var ev    = context as TakeOrderEvent;
            if (agent == null || ev == null) return WorkResult.Cancelled;

            var seat     = ev.Seat;
            var customer = seat.GetModule<SeatModule>()?.AssignedAgent as CustomerAgent;

            await agent.MoveAsync(seat.GetNearestPoint(executor.transform.position), ctx.Token);
            await agent.DoWorkAsync(seat, ctx.Token);

            var ticket = customer?.PlaceOrder(seat);
            if (ticket != null)
                OrderManager.Instance?.Register(ticket);

            return WorkResult.Completed;
        }
    }
}
```

---

- [ ] **Step 1-13: CustomerCycleSequenceSO — RunAsync (WaitIfPausedAsync는 Task 3에서 추가)**

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

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            if (_steps == null) return WorkResult.Completed;

            foreach (var step in _steps)
            {
                if (step == null) continue;

                var stepResult = await step.ExecuteAsync(executor, context, ctx);
                step.OnResult(stepResult, executor, context);

                if (stepResult == WorkResult.Cancelled)           return WorkResult.Cancelled;
                if (stepResult == WorkResult.ExternallyCompleted) return WorkResult.Completed;
            }

            return WorkResult.Completed;
        }
    }
}
```

---

- [ ] **Step 1-14: Unity 컴파일 확인**

Unity 에디터 Console에 컴파일 오류가 없는지 확인한다.  
오류가 있으면 해당 파일을 수정한다.

---

- [ ] **Step 1-15: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/EatWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/AgentActionWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/WaitForFoodWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/ExitWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/PayAtCounterWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/TakeSeatWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/CustomerCycleSequenceSO.cs"
git commit -m "refactor: seal WorkSO.ExecuteAsync, add RunAsync — eliminate boilerplate try/catch"
```

---

## Task 2: WorkExecutionContext Pause/Resume + SchedulingModule 노출

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs`

---

- [ ] **Step 2-1: WorkExecutionContext — Pause/Resume/WaitIfPausedAsync 추가**

`Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs` 전체 내용:

```csharp
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace BBJ.Work
{
    public sealed class WorkExecutionContext : IDisposable
    {
        private readonly CancellationTokenSource _cancelCts;
        private readonly CancellationTokenSource _completeCts;
        private readonly CancellationTokenSource _linkedCts;
        private UniTaskCompletionSource          _pauseGate;

        public CancellationToken Token               => _linkedCts.Token;
        public bool              WasExternallyCompleted => _completeCts.IsCancellationRequested;
        public bool              IsPaused            => _pauseGate != null;

        public WorkExecutionContext()
        {
            _cancelCts   = new CancellationTokenSource();
            _completeCts = new CancellationTokenSource();
            _linkedCts   = CancellationTokenSource.CreateLinkedTokenSource(
                               _cancelCts.Token, _completeCts.Token);
        }

        internal void ForceComplete() => _completeCts.Cancel();
        internal void HardCancel()    => _cancelCts.Cancel();

        public void Pause()
        {
            _pauseGate ??= new UniTaskCompletionSource();
        }

        public void Resume()
        {
            _pauseGate?.TrySetResult();
            _pauseGate = null;
        }

        public UniTask WaitIfPausedAsync(CancellationToken ct = default)
        {
            if (_pauseGate == null) return UniTask.CompletedTask;
            return _pauseGate.Task.AttachExternalCancellation(ct);
        }

        public void Dispose()
        {
            _pauseGate?.TrySetCanceled();
            _pauseGate = null;
            _cancelCts.Dispose();
            _completeCts.Dispose();
            _linkedCts.Dispose();
        }
    }
}
```

---

- [ ] **Step 2-2: SchedulingModule — Pause()/Resume() 공개 추가**

`Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs`에서  
`CancelWork()` 메서드 아래에 두 줄을 추가한다:

```csharp
public void Pause()  => _execCtx?.Pause();
public void Resume() => _execCtx?.Resume();
```

---

- [ ] **Step 2-3: Unity 컴파일 확인**

Console에 오류 없음 확인.

---

- [ ] **Step 2-4: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/WorkExecutionContext.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs"
git commit -m "feat: add Pause/Resume to WorkExecutionContext and SchedulingModule"
```

---

## Task 3: CustomerCycleSequenceSO — 스텝 사이 WaitIfPausedAsync

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CustomerCycleSequenceSO.cs`

---

- [ ] **Step 3-1: CustomerCycleSequenceSO — foreach 앞에 WaitIfPausedAsync 추가**

`RunAsync`의 foreach 루프 안, `if (step == null) continue;` 다음 줄에 추가:

```csharp
protected override async UniTask<WorkResult> RunAsync(
    ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
{
    if (_steps == null) return WorkResult.Completed;

    foreach (var step in _steps)
    {
        if (step == null) continue;

        await ctx.WaitIfPausedAsync(ctx.Token);   // ← 이 줄 추가

        var stepResult = await step.ExecuteAsync(executor, context, ctx);
        step.OnResult(stepResult, executor, context);

        if (stepResult == WorkResult.Cancelled)           return WorkResult.Cancelled;
        if (stepResult == WorkResult.ExternallyCompleted) return WorkResult.Completed;
    }

    return WorkResult.Completed;
}
```

---

- [ ] **Step 3-2: Unity 에디터에서 pause 동작 수동 검증**

Play Mode 진입 → 손님 사이클 실행 중 `SchedulingModule.Pause()` 호출(임시 테스트 코드 또는 Debug 버튼) → 다음 스텝 진행 안 됨 확인 → `Resume()` 호출 → 진행 재개 확인.

---

- [ ] **Step 3-3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/CustomerCycleSequenceSO.cs"
git commit -m "feat: pause between sequence steps via WaitIfPausedAsync"
```

---

## Task 4: 플레이어 클릭 주문 흐름

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs`

---

- [ ] **Step 4-1: CustomerAgent — AssignedServer 추가, OnPlayerInteract 제거**

`CustomerAgent.cs`에서 다음을 변경한다.

**추가할 프로퍼티/메서드:**
```csharp
public ModuleOwner AssignedServer { get; private set; }
public void SetAssignedServer(ModuleOwner server) => AssignedServer = server;
```

**제거할 메서드:**
```csharp
// 아래 메서드 전체 제거
public void OnPlayerInteract()
{
    (ActiveTicket?.ReservedBy as ModuleOwner)?.GetModule<SchedulingModule>()?.ResolveWork();
    if (OrderPlaced && !FoodServed) OnFoodServed();
}
```

**ResetItem에 AssignedServer 초기화 추가:**
```csharp
public override void ResetItem()
{
    GetModule<SchedulingModule>()?.CancelWork();
    AssignedSeat     = null;
    AssignedServer   = null;   // ← 추가
    ActiveTicket     = null;
    SelectedFood     = null;
    OrderPlaced      = false;
    FoodServed       = false;
    PaymentDone      = false;
    IsAwaitingOrder  = false;
}
```

---

- [ ] **Step 4-2: TakeOrderWorkSO — RunAsync에 SetAssignedServer 추가**

`Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs` 전체 내용:

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
        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var agent    = executor as IActionDispatcher;
            var ev       = context as TakeOrderEvent;
            if (agent == null || ev == null) return WorkResult.Cancelled;

            var seat     = ev.Seat;
            var customer = seat.GetModule<SeatModule>()?.AssignedAgent as CustomerAgent;

            customer?.SetAssignedServer(executor);

            await agent.MoveAsync(seat.GetNearestPoint(executor.transform.position), ctx.Token);
            await agent.DoWorkAsync(seat, ctx.Token);

            var ticket = customer?.PlaceOrder(seat);
            if (ticket != null)
                OrderManager.Instance?.Register(ticket);

            customer?.SetAssignedServer(null);
            return WorkResult.Completed;
        }
    }
}
```

---

- [ ] **Step 4-3: WaitOrderWorkSO — OnResult 추가 (ExternallyCompleted 처리)**

`Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs` 전체 내용:

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

        protected override async UniTask<WorkResult> RunAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx)
        {
            var customer = executor as CustomerAgent;
            var agent    = executor as IActionDispatcher;
            var seat     = customer?.AssignedSeat;
            if (customer == null || agent == null || seat == null) return WorkResult.Cancelled;

            customer.SetAwaitingOrder(true);
            _dispatchTable?.Dispatch(OrderWorkPhase.ReadyForServer, new TakeOrderEvent(seat), ScheduleManager.Instance);
            try
            {
                await agent.WaitUntilAsync(() => customer.OrderPlaced, ctx.Token, _patienceLimit);
                return WorkResult.Completed;
            }
            finally
            {
                customer.SetAwaitingOrder(false);
            }
        }

        public override void OnResult(WorkResult result, ModuleOwner executor, GameEvent context)
        {
            if (result != WorkResult.ExternallyCompleted) return;

            var customer = executor as CustomerAgent;
            var seat     = customer?.AssignedSeat;

            customer?.AssignedServer?.GetModule<SchedulingModule>()?.CancelWork();

            var ticket = customer?.PlaceOrder(seat);
            if (ticket != null)
                OrderManager.Instance?.Register(ticket);
        }
    }
}
```

---

- [ ] **Step 4-4: Unity 컴파일 확인**

Console에 오류 없음 확인.

---

- [ ] **Step 4-5: Unity 에디터에서 플레이어 클릭 흐름 수동 검증**

1. Play Mode 진입
2. 손님이 `WaitOrder` 상태(IsAwaitingOrder = true)가 될 때까지 대기
3. 스태프가 TakeOrder를 수행 중인지 확인
4. `customer.GetModule<SchedulingModule>().ResolveWork()` 호출 (임시 테스트 버튼 또는 Inspector 디버그)
5. 확인: 스태프 TakeOrder 취소됨 → OrderManager에 티켓 등록됨 → 손님 OrderPlaced = true

---

- [ ] **Step 4-6: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/TakeOrderWorkSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/WaitOrderWorkSO.cs"
git commit -m "feat: player click order flow — AssignedServer tracking, WaitOrderWorkSO.OnResult"
```

---

## Task 5: CookingStartEvent + CookWorkSO 미니게임 pause

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WorkEvents.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs`

---

- [ ] **Step 5-1: WorkEvents.cs — CookingStartEvent 추가**

`Assets/00. Work/BBJ/02. Scripts/Work/WorkEvents.cs`에 `CookingStartEvent` 추가:

```csharp
using BBJ.Order;
using BBJ.WorkplaceSystem;
using Gamelib.EventSystem;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    public class TakeOrderEvent : GameEvent
    {
        public Workplace Seat { get; }
        public TakeOrderEvent(Workplace seat) => Seat = seat;
    }

    public class OrderWorkEvent : GameEvent
    {
        public OrderTicket  Ticket       { get; }
        public OrderManager OrderManager { get; }

        public OrderWorkEvent(OrderTicket ticket, OrderManager orderManager)
        {
            Ticket       = ticket;
            OrderManager = orderManager;
        }
    }

    public class CookingStartEvent : GameEvent
    {
        public OrderTicket Ticket { get; }
        public ModuleOwner Staff  { get; }

        public CookingStartEvent(OrderTicket ticket, ModuleOwner staff)
        {
            Ticket = ticket;
            Staff  = staff;
        }
    }
}
```

---

- [ ] **Step 5-2: CookWorkSO — DoWorkAsync → pause 방식으로 교체**

`_cookingStartChannel` 필드 추가, `DoWorkAsync` 제거, pause 트리거 삽입.  
`Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs` 전체 내용:

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
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "CookWork", menuName = "Tycoon/Work/Cook")]
    public class CookWorkSO : WorkSO
    {
        [SerializeField] private WorkplaceTypeSO     _kitchenType;
        [SerializeField] private WorkplaceRegisterSO _workplaceRegister;
        [SerializeField] private EventChannelSO      _cookingStartChannel;

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

                _cookingStartChannel?.RaiseEvent(new CookingStartEvent(ev.Ticket, executor));
                ctx.Pause();
                await ctx.WaitIfPausedAsync(ctx.Token);

                return WorkResult.Completed;
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
            if (ev == null) return;
            if (result != WorkResult.Cancelled)
                ev.OrderManager.NotifyComplete(ev.Ticket, executor);
            else
                ev.OrderManager.NotifyReleased(ev.Ticket, executor);
        }
    }
}
```

---

- [ ] **Step 5-3: CookWorkSO 인스펙터 연결**

Unity Inspector에서 CookWork SO 에셋을 열어  
`_cookingStartChannel` 필드에 `CookingStartEvent`용 EventChannelSO 에셋을 연결한다.  
(기존 SO가 없으면 `Create > Tycoon > EventChannel`로 새로 생성)

---

- [ ] **Step 5-4: Unity 컴파일 확인**

Console에 오류 없음 확인.

---

- [ ] **Step 5-5: Unity 에디터에서 CookingStartEvent 발행 확인**

1. Play Mode 진입
2. 스태프가 CookWork를 시작할 때까지 대기
3. CookingStartEvent 채널에 임시 리스너를 붙여 이벤트 수신 확인
4. 이벤트 수신 후 스태프가 정지 상태인지 확인 (pause 적용)
5. `staff.GetModule<SchedulingModule>().Resume()` 호출 → 스태프 재개 + OnResult 호출 확인

---

- [ ] **Step 5-6: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/WorkEvents.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs"
git commit -m "feat: CookWorkSO triggers mini-game via pause — CookingStartEvent dispatch"
```
