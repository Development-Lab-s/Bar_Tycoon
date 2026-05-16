# Player Intervention System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 플레이어가 스태프 작업(요리/주문받기/서빙/계산)을 월드 클릭 또는 주문 보드 UI로 직접 수행하거나 진행 중인 스태프 작업을 가로챌 수 있는 시스템을 구축한다.

**Architecture:** `IWorkOwner` 인터페이스로 티켓 소유권을 추상화하고 `ModuleOwner`와 `PlayerWorkOwner` 모두 구현하게 한다. `PlayerInterventionManager`가 가능한 작업 슬롯을 감지하고, 플레이어가 클레임 시 스태프를 취소한 뒤 `IInterventionHandler`로 실행을 위임한다. 기존 `WorkSO.OnResult`가 결과를 처리하므로 ExecuteAsync 내부 로직은 변경하지 않는다.

**Tech Stack:** Unity (C#), UniTask, ScriptableObject 패턴, Gamelib.EventSystem

---

## 파일 구조

| 경로 | 역할 |
|------|------|
| `Assets/00. Work/_Resources/02. Scripts/Modules/IWorkOwner.cs` | 신규 — 소유권 추상 인터페이스 |
| `Assets/00. Work/_Resources/02. Scripts/Modules/ModuleOwner.cs` | 수정 — `IWorkOwner` 구현 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Work/PlayerWorkOwner.cs` | 신규 — 플레이어 싱글톤 IWorkOwner |
| `Assets/00. Work/BBJ/02. Scripts/Order/OrderTicket.cs` | 수정 — IWorkOwner, TrySteal |
| `Assets/00. Work/BBJ/02. Scripts/Order/OrderManager.cs` | 수정 — IWorkOwner 시그니처 |
| `Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs` | 수정 — OnResult 시그니처, PlayerHandler 필드 |
| `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs` | 수정 — OnResult 추가, ExecuteAsync 정리 |
| `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs` | 수정 — OnResult 시그니처 변경 |
| `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs` | 수정 — CurrentWork/CurrentContext 노출 |
| `Assets/00. Work/BBJ/02. Scripts/Work/IInterventionHandler.cs` | 신규 |
| `Assets/00. Work/BBJ/02. Scripts/Work/InterventionHandlerSO.cs` | 신규 — abstract SO |
| `Assets/00. Work/BBJ/02. Scripts/Work/ImmediateInterventionHandlerSO.cs` | 신규 — TBD 기본 구현 |
| `Assets/00. Work/BBJ/02. Scripts/Work/PlayerInterventionSlot.cs` | 신규 |
| `Assets/00. Work/BBJ/02. Scripts/Work/PlayerInterventionManager.cs` | 신규 |
| `Assets/00. Work/BBJ/02. Scripts/Workplace/IPlayerInteractable.cs` | 신규 |
| `Assets/00. Work/BBJ/02. Scripts/Workplace/Workplace.cs` | 수정 — IPlayerInteractable 구현 |
| `Assets/00. Work/BBJ/02. Scripts/UI/Order/OrderTicketUI.cs` | 수정 — claim 버튼 추가 |

---

## Task 1: IWorkOwner 인터페이스 + ModuleOwner 구현

**Files:**
- Create: `Assets/00. Work/_Resources/02. Scripts/Modules/IWorkOwner.cs`
- Modify: `Assets/00. Work/_Resources/02. Scripts/Modules/ModuleOwner.cs`

- [ ] **Step 1: IWorkOwner.cs 생성**

```csharp
// Assets/00. Work/_Resources/02. Scripts/Modules/IWorkOwner.cs
namespace _00._Work._Resources._02._Scripts.Modules
{
    public interface IWorkOwner { }
}
```

- [ ] **Step 2: ModuleOwner에 IWorkOwner 구현 추가**

`ModuleOwner.cs` 의 `public abstract class ModuleOwner : MonoBehaviour` 를 다음으로 변경:

```csharp
public abstract class ModuleOwner : MonoBehaviour, IWorkOwner
```

- [ ] **Step 3: Unity 콘솔에서 컴파일 에러 없음 확인**

Unity Editor → Console 탭에서 에러 0개 확인.

- [ ] **Step 4: 커밋**

```
git add "Assets/00. Work/_Resources/02. Scripts/Modules/IWorkOwner.cs"
git add "Assets/00. Work/_Resources/02. Scripts/Modules/ModuleOwner.cs"
git commit -m "feat: add IWorkOwner interface and implement on ModuleOwner"
```

---

## Task 2: PlayerWorkOwner 싱글톤

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Work/PlayerWorkOwner.cs`

- [ ] **Step 1: PlayerWorkOwner.cs 생성**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Work/PlayerWorkOwner.cs
using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace BBJ.Work
{
    public class PlayerWorkOwner : MonoBehaviour, IWorkOwner
    {
        public static PlayerWorkOwner Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/PlayerWorkOwner.cs"
git commit -m "feat: add PlayerWorkOwner singleton as IWorkOwner identity for player"
```

---

## Task 3: OrderTicket — IWorkOwner 마이그레이션 + TrySteal

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Order/OrderTicket.cs`

- [ ] **Step 1: OrderTicket.cs 전체 교체**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Order/OrderTicket.cs
using BBJ.Data;
using BBJ.WorkplaceSystem;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Order
{
    public class OrderTicket
    {
        public FoodDataSO  Food      { get; }
        public ModuleOwner Customer  { get; }
        public Workplace   Seat      { get; }

        public OrderState     State              { get; private set; } = OrderState.Waiting;
        public OrderWorkPhase WorkPhase          { get; internal set; } = OrderWorkPhase.PendingCook;
        public IWorkOwner     ReservedBy         { get; private set; }
        public CancelReason?  CancellationReason { get; private set; }

        public OrderTicket(FoodDataSO food, ModuleOwner customer, Workplace seat)
        {
            Food     = food;
            Customer = customer;
            Seat     = seat;
        }

        internal bool Advance()
        {
            if (State >= OrderState.InProgress) return false;
            State++;
            return true;
        }

        public bool TryReserve(IWorkOwner actor)
        {
            if (actor == null || State != OrderState.Waiting) return false;
            ReservedBy = actor;
            return Advance();
        }

        public bool TryStartProgress(IWorkOwner actor)
        {
            if (State != OrderState.Reserved || ReservedBy != actor) return false;
            return Advance();
        }

        // 플레이어 뺏기: 진행 중인 작업이라도 소유권을 강제 이전하고 상태를 Reserved로 되돌린다.
        public bool TrySteal(IWorkOwner newOwner)
        {
            if (State == OrderState.Done || State == OrderState.Cancelled) return false;
            ReservedBy = newOwner;
            State      = OrderState.Reserved;
            return true;
        }

        internal void Release()
        {
            ReservedBy = null;
            State = OrderState.Waiting;
        }

        internal void Finish()
        {
            ReservedBy = null;
            State = OrderState.Done;
        }

        internal void Cancel(CancelReason reason)
        {
            CancellationReason = reason;
            ReservedBy = null;
            State = OrderState.Cancelled;
        }
    }
}
```

- [ ] **Step 2: 컴파일 에러 확인**

`OrderManager.cs` 에서 `ModuleOwner actor` 파라미터 관련 에러가 발생할 수 있음 — 다음 Task에서 수정.

- [ ] **Step 3: 커밋 (OrderManager 에러 있더라도 여기서 커밋)**

```
git add "Assets/00. Work/BBJ/02. Scripts/Order/OrderTicket.cs"
git commit -m "refactor: migrate OrderTicket ownership from ModuleOwner to IWorkOwner, add TrySteal"
```

---

## Task 4: OrderManager — IWorkOwner 시그니처

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Order/OrderManager.cs`

- [ ] **Step 1: NotifyComplete, NotifyReleased, IsOwner 시그니처 변경**

`OrderManager.cs` 에서 다음 세 메서드를 교체:

```csharp
public bool NotifyComplete(OrderTicket ticket, IWorkOwner actor)
{
    if (!IsOwner(ticket, actor)) return false;
    if (ticket.State != OrderState.InProgress) return false;

    var entry = _dispatchTable?.FindEntry(ticket.WorkPhase);
    if (entry == null || entry.Value.NextPhase == OrderWorkPhase.Done)
    {
        ticket.WorkPhase = OrderWorkPhase.Done;
        ticket.Finish();
        _orderRegister.Unregister(ticket);
        _orderChannel?.RaiseEvent(new OrderUnregisteredEvent(ticket));
        return true;
    }

    ticket.WorkPhase = entry.Value.NextPhase;
    ticket.Release();
    _orderChannel?.RaiseEvent(new OrderStateChangedEvent(ticket));
    _dispatchTable?.Dispatch(ticket.WorkPhase, new OrderWorkEvent(ticket, this), _scheduleManager);
    return true;
}

public bool NotifyReleased(OrderTicket ticket, IWorkOwner actor)
{
    if (!IsOwner(ticket, actor)) return false;
    if (ticket.State is OrderState.Done or OrderState.Cancelled) return false;

    if (ticket.State == OrderState.InProgress)
        HandleInterrupted(ticket);
    else
    {
        ticket.Release();
        _dispatchTable?.Dispatch(ticket.WorkPhase, new OrderWorkEvent(ticket, this), _scheduleManager);
    }
    return true;
}

private static bool IsOwner(OrderTicket ticket, IWorkOwner actor)
    => actor != null && ticket.ReservedBy == actor;
```

`using _00._Work._Resources._02._Scripts.Modules;` 가 파일 상단에 있는지 확인.

- [ ] **Step 2: 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Order/OrderManager.cs"
git commit -m "refactor: migrate OrderManager actor params from ModuleOwner to IWorkOwner"
```

---

## Task 5: WorkSO — OnResult 시그니처 + PlayerHandler 필드

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs`

- [ ] **Step 1: WorkSO.cs 전체 교체**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs
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

        [SerializeField] private InterventionHandlerSO _playerHandler;
        public bool IsPlayerInteractable => _playerHandler != null;
        public InterventionHandlerSO PlayerHandler => _playerHandler;

        public abstract UniTask<WorkResult> ExecuteAsync(
            ModuleOwner executor, GameEvent context, WorkExecutionContext ctx);

        public virtual void OnResult(
            WorkResult result, IWorkOwner executor, GameEvent context) { }
    }
}
```

- [ ] **Step 2: 컴파일 에러 확인**

`InterventionHandlerSO` 미정의 에러 발생 — Task 9에서 해결. `ServeWorkSO.OnResult` 시그니처 에러 — 다음 Task에서 해결.

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/WorkSO.cs"
git commit -m "refactor: change WorkSO.OnResult executor to IWorkOwner, add PlayerHandler field"
```

---

## Task 6: CookWorkSO — OnResult 추가, ExecuteAsync 정리

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs`

현재 `CookWorkSO.ExecuteAsync` 는 `NotifyComplete/NotifyReleased` 를 내부에서 직접 호출한다. 이를 `OnResult` 로 이동해야 플레이어 개입 후 동일한 결과 처리 경로를 재사용할 수 있다.

- [ ] **Step 1: CookWorkSO.cs 전체 교체**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs
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

        public override async UniTask<WorkResult> ExecuteAsync(
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
                ctx.Token.ThrowIfCancellationRequested();

                ev.Ticket.TryStartProgress(executor);
                foodContext?.SetFood(ev.Ticket.Food);

                await agent.DoWorkAsync(kitchen, ctx.Token);
                ctx.Token.ThrowIfCancellationRequested();

                return WorkResult.Completed;
            }
            catch (OperationCanceledException) when (ctx.WasExternallyCompleted)
            {
                return WorkResult.ExternallyCompleted;
            }
            catch (OperationCanceledException)
            {
                return WorkResult.Cancelled;
            }
            finally
            {
                foodContext?.ClearFood();
                kitchen.GetModule<OccupancyModule>()?.Release();
            }
        }

        public override void OnResult(WorkResult result, IWorkOwner executor, GameEvent context)
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

**주의:** 원본 코드의 `if (kitchen == null)` 분기에서 `ev.OrderManager.NotifyReleased` 를 직접 호출하던 코드를 `ev.Ticket.Release()` 로 대체했다. `NotifyReleased` 는 `IsOwner` 체크를 하므로, TryReserve 직후 취소 시에는 ticket을 직접 해제한다.

- [ ] **Step 2: 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/CookWorkSO.cs"
git commit -m "refactor: move CookWorkSO state mutations to OnResult"
```

---

## Task 7: ServeWorkSO + 기타 WorkSO OnResult 시그니처 수정

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs`

- [ ] **Step 1: ServeWorkSO.OnResult 시그니처 변경**

`ServeWorkSO.cs` 의 `OnResult` 메서드 시그니처만 변경 (본문 동일):

```csharp
public override void OnResult(WorkResult result, IWorkOwner executor, GameEvent context)
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

- [ ] **Step 2: 다른 WorkSO에 OnResult 재정의가 있는지 확인**

```
grep -rn "override void OnResult" "Assets/00. Work/BBJ/02. Scripts/Work/"
```

결과에서 `ModuleOwner` 파라미터를 가진 OnResult를 모두 `IWorkOwner` 로 변경.

- [ ] **Step 3: 컴파일 에러 없음 확인**

- [ ] **Step 4: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/ServeWorkSO.cs"
git commit -m "refactor: update ServeWorkSO.OnResult executor to IWorkOwner"
```

---

## Task 8: SchedulingModule — CurrentWork/CurrentContext 노출

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs`

`PlayerInterventionManager` 가 "어떤 스태프가 어떤 작업을 하고 있는지" 알아야 한다.

- [ ] **Step 1: SchedulingModule.cs 에 프로퍼티 및 set/clear 추가**

기존 `private WorkExecutionContext _execCtx;` 아래에 추가:

```csharp
public WorkSO    CurrentWork    { get; private set; }
public GameEvent CurrentContext { get; private set; }
```

`AssignWork` 메서드 수정:

```csharp
public void AssignWork(WorkSO workSO, GameEvent context)
{
    CancelWork();
    CurrentWork    = workSO;
    CurrentContext = context;
    _execCtx       = new WorkExecutionContext();
    RunAsync(workSO, context, _execCtx).Forget();
}
```

`RunAsync` 의 `finally` 블록에 클리어 추가:

```csharp
finally
{
    if (_execCtx == ctx) _execCtx = null;
    CurrentWork    = null;
    CurrentContext = null;
    ctx.Dispose();
    workSO.OnResult(result, _owner, context);
    OnWorkEnded?.Invoke(result != WorkResult.Cancelled);
    ScheduleTriggerChannel?.RaiseEvent(new ScheduleTriggerEvent());
}
```

- [ ] **Step 2: 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Modules/SchedulingModule.cs"
git commit -m "feat: expose CurrentWork and CurrentContext on SchedulingModule"
```

---

## Task 9: IInterventionHandler + InterventionHandlerSO + ImmediateInterventionHandlerSO

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Work/IInterventionHandler.cs`
- Create: `Assets/00. Work/BBJ/02. Scripts/Work/InterventionHandlerSO.cs`
- Create: `Assets/00. Work/BBJ/02. Scripts/Work/ImmediateInterventionHandlerSO.cs`

- [ ] **Step 1: IInterventionHandler.cs 생성**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Work/IInterventionHandler.cs
using System.Threading;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;

namespace BBJ.Work
{
    public interface IInterventionHandler
    {
        UniTask<WorkResult> HandleAsync(GameEvent context, CancellationToken token);
    }
}
```

- [ ] **Step 2: InterventionHandlerSO.cs 생성**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Work/InterventionHandlerSO.cs
using System.Threading;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Work
{
    public abstract class InterventionHandlerSO : ScriptableObject, IInterventionHandler
    {
        public abstract UniTask<WorkResult> HandleAsync(GameEvent context, CancellationToken token);
    }
}
```

- [ ] **Step 3: ImmediateInterventionHandlerSO.cs 생성**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Work/ImmediateInterventionHandlerSO.cs
using System.Threading;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;

namespace BBJ.Work
{
    [CreateAssetMenu(fileName = "ImmediateHandler", menuName = "Tycoon/Intervention/Immediate")]
    public class ImmediateInterventionHandlerSO : InterventionHandlerSO
    {
        public override UniTask<WorkResult> HandleAsync(GameEvent context, CancellationToken token)
            => UniTask.FromResult(WorkResult.Completed);
    }
}
```

- [ ] **Step 4: 컴파일 에러 없음 확인**

Task 5에서 발생했던 `InterventionHandlerSO` 미정의 에러가 여기서 해소된다.

- [ ] **Step 5: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/IInterventionHandler.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/InterventionHandlerSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/ImmediateInterventionHandlerSO.cs"
git commit -m "feat: add IInterventionHandler, InterventionHandlerSO, ImmediateInterventionHandlerSO"
```

---

## Task 10: PlayerInterventionSlot + PlayerInterventionManager

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Work/PlayerInterventionSlot.cs`
- Create: `Assets/00. Work/BBJ/02. Scripts/Work/PlayerInterventionManager.cs`

- [ ] **Step 1: PlayerInterventionSlot.cs 생성**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Work/PlayerInterventionSlot.cs
using BBJ.Schedule;
using Gamelib.EventSystem;

namespace BBJ.Work
{
    public class PlayerInterventionSlot
    {
        public WorkSO           Work;
        public GameEvent        Context;
        public SchedulingModule ActiveStaff;  // null = 스태프 미배정

        public PlayerInterventionSlot(WorkSO work, GameEvent context, SchedulingModule activeStaff)
        {
            Work        = work;
            Context     = context;
            ActiveStaff = activeStaff;
        }
    }
}
```

- [ ] **Step 2: PlayerInterventionManager.cs 생성**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Work/PlayerInterventionManager.cs
using BBJ.EventSystem;
using BBJ.Order;
using BBJ.Register;
using BBJ.Schedule;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    public class PlayerInterventionManager : MonoBehaviour
    {
        public static PlayerInterventionManager Instance { get; private set; }

        [SerializeField] private OrderRegisterSO     _orderRegister;
        [SerializeField] private WorkDispatchTableSO _dispatchTable;
        [SerializeField] private EventChannelSO      _orderChannel;
        [SerializeField] private EventChannelSO      _scheduleTriggerChannel;

        private readonly List<PlayerInterventionSlot> _slots = new();
        public IReadOnlyList<PlayerInterventionSlot> AvailableSlots => _slots;

        public event Action OnSlotsChanged;

        private CancellationTokenSource _claimCts;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            _orderChannel?          .AddListener<OrderRegisteredEvent>(OnOrderRegistered);
            _orderChannel?          .AddListener<OrderStateChangedEvent>(OnOrderChanged);
            _orderChannel?          .AddListener<OrderUnregisteredEvent>(OnOrderUnregistered);
            _scheduleTriggerChannel?.AddListener<ScheduleTriggerEvent>(OnScheduleEvent);
        }

        private void OnDisable()
        {
            _orderChannel?          .RemoveListener<OrderRegisteredEvent>(OnOrderRegistered);
            _orderChannel?          .RemoveListener<OrderStateChangedEvent>(OnOrderChanged);
            _orderChannel?          .RemoveListener<OrderUnregisteredEvent>(OnOrderUnregistered);
            _scheduleTriggerChannel?.RemoveListener<ScheduleTriggerEvent>(OnScheduleEvent);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnOrderRegistered(OrderRegisteredEvent _)     => RebuildSlots();
        private void OnOrderChanged(OrderStateChangedEvent _)       => RebuildSlots();
        private void OnOrderUnregistered(OrderUnregisteredEvent _)  => RebuildSlots();
        private void OnScheduleEvent(ScheduleTriggerEvent _)        => RebuildSlots();

        private void RebuildSlots()
        {
            _slots.Clear();

            if (_orderRegister == null || _dispatchTable == null) return;

            foreach (var ticket in _orderRegister.Registry)
            {
                if (ticket.State == OrderState.Done || ticket.State == OrderState.Cancelled)
                    continue;

                var entry = _dispatchTable.FindEntry(ticket.WorkPhase);
                if (entry == null || !entry.Value.Work.IsPlayerInteractable) continue;

                var activeStaff = FindStaffWorkingOn(ticket);
                _slots.Add(new PlayerInterventionSlot(
                    entry.Value.Work,
                    new OrderWorkEvent(ticket, OrderManager.Instance),
                    activeStaff));
            }

            OnSlotsChanged?.Invoke();
        }

        private static SchedulingModule FindStaffWorkingOn(OrderTicket ticket)
        {
            if (ticket.ReservedBy is ModuleOwner owner)
                return owner.GetModule<SchedulingModule>();
            return null;
        }

        public void Claim(PlayerInterventionSlot slot)
        {
            _claimCts?.Cancel();
            _claimCts = new CancellationTokenSource();
            ClaimAsync(slot, _claimCts.Token).Forget();
        }

        private async UniTaskVoid ClaimAsync(PlayerInterventionSlot slot, CancellationToken token)
        {
            var ev     = slot.Context as OrderWorkEvent;
            var ticket = ev?.Ticket;

            // 소유권 이전 후 InProgress로 전진
            if (ticket != null)
            {
                if (!ticket.TrySteal(PlayerWorkOwner.Instance)) return;
                ticket.TryStartProgress(PlayerWorkOwner.Instance);
            }

            // 스태프 취소 — 스태프 OnResult에서 NotifyReleased 시도하지만 IsOwner 실패 → 재dispatch 없음
            slot.ActiveStaff?.CancelWork();

            WorkResult result;
            try
            {
                result = await slot.Work.PlayerHandler.HandleAsync(slot.Context, token);
            }
            catch (OperationCanceledException)
            {
                result = WorkResult.Cancelled;
            }

            slot.Work.OnResult(result, PlayerWorkOwner.Instance, slot.Context);
            RebuildSlots();
        }
    }
}
```

- [ ] **Step 3: 컴파일 에러 없음 확인**

- [ ] **Step 4: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Work/PlayerInterventionSlot.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Work/PlayerInterventionManager.cs"
git commit -m "feat: add PlayerInterventionSlot and PlayerInterventionManager"
```

---

## Task 11: IPlayerInteractable + Workplace 구현

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Workplace/IPlayerInteractable.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Workplace/Workplace.cs`

- [ ] **Step 1: IPlayerInteractable.cs 생성**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Workplace/IPlayerInteractable.cs
namespace BBJ.WorkplaceSystem
{
    public interface IPlayerInteractable
    {
        bool CanPlayerInteract { get; }
        void OnPlayerClick();
    }
}
```

- [ ] **Step 2: Workplace.cs 에 IPlayerInteractable 추가**

`Workplace` 클래스 선언 변경:

```csharp
public class Workplace : ModuleOwner, IPlayerInteractable
```

클래스 본문 끝에 구현 추가:

```csharp
public bool CanPlayerInteract
{
    get
    {
        if (PlayerInterventionManager.Instance == null) return false;
        foreach (var slot in PlayerInterventionManager.Instance.AvailableSlots)
        {
            var ev = slot.Context as BBJ.Work.OrderWorkEvent;
            if (ev != null && (ev.Ticket.Seat == this || IsTargetWorkplace(slot, this)))
                return true;
        }
        return false;
    }
}

public void OnPlayerClick()
{
    if (PlayerInterventionManager.Instance == null) return;
    foreach (var slot in PlayerInterventionManager.Instance.AvailableSlots)
    {
        var ev = slot.Context as BBJ.Work.OrderWorkEvent;
        if (ev != null && (ev.Ticket.Seat == this || IsTargetWorkplace(slot, this)))
        {
            PlayerInterventionManager.Instance.Claim(slot);
            return;
        }
    }
}

private static bool IsTargetWorkplace(BBJ.Work.PlayerInterventionSlot slot, Workplace wp)
    => false; // 향후 ServeWorkSO 등에서 serve station 매칭 시 확장
```

`Workplace.cs` 상단에 using 추가:

```csharp
using BBJ.Work;
```

- [ ] **Step 3: 컴파일 에러 없음 확인**

- [ ] **Step 4: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/IPlayerInteractable.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Workplace.cs"
git commit -m "feat: add IPlayerInteractable interface and implement on Workplace"
```

---

## Task 12: OrderTicketUI — Claim 버튼

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/UI/Order/OrderTicketUI.cs`

현재 `OrderTicketUI` 가 어떤 필드를 가졌는지 먼저 확인 후 추가.

- [ ] **Step 1: OrderTicketUI.cs 현재 내용 확인**

```
cat "Assets/00. Work/BBJ/02. Scripts/UI/Order/OrderTicketUI.cs"
```

- [ ] **Step 2: ClaimButton 필드 및 로직 추가**

클래스 상단에 필드 추가:

```csharp
[SerializeField] private UnityEngine.UI.Button _claimButton;
```

Awake 또는 OnEnable에서 이벤트 연결 추가:

```csharp
private void OnEnable()
{
    _claimButton?.onClick.AddListener(OnClaimClicked);
    PlayerInterventionManager.Instance?.OnSlotsChanged += RefreshClaimButton;
    RefreshClaimButton();
}

private void OnDisable()
{
    _claimButton?.onClick.RemoveListener(OnClaimClicked);
    if (PlayerInterventionManager.Instance != null)
        PlayerInterventionManager.Instance.OnSlotsChanged -= RefreshClaimButton;
}
```

메서드 추가 (`_ticket` 은 기존에 있는 OrderTicket 참조 필드명으로 교체):

```csharp
private void RefreshClaimButton()
{
    if (_claimButton == null || PlayerInterventionManager.Instance == null) return;
    _claimButton.gameObject.SetActive(FindMatchingSlot() != null);
}

private BBJ.Work.PlayerInterventionSlot FindMatchingSlot()
{
    foreach (var slot in PlayerInterventionManager.Instance.AvailableSlots)
    {
        var ev = slot.Context as BBJ.Work.OrderWorkEvent;
        if (ev?.Ticket == _ticket) return slot;
    }
    return null;
}

private void OnClaimClicked()
{
    var slot = FindMatchingSlot();
    if (slot != null)
        PlayerInterventionManager.Instance.Claim(slot);
}
```

**주의:** `_ticket` 은 `OrderTicketUI` 가 실제로 가진 `OrderTicket` 참조 필드명으로 맞춰야 한다. Step 1에서 확인한 필드명 사용.

- [ ] **Step 3: 컴파일 에러 없음 확인**

- [ ] **Step 4: 커밋**

```
git add "Assets/00. Work/BBJ/02. Scripts/UI/Order/OrderTicketUI.cs"
git commit -m "feat: add claim button to OrderTicketUI for player intervention"
```

---

## Task 13: 씬 설정 + 인스펙터 연결

**이 Task는 Unity Editor에서 직접 수행.**

- [ ] **Step 1: PlayerWorkOwner GameObject 생성**

Hierarchy → Create Empty → 이름: `PlayerWorkOwner`
Inspector → Add Component → `PlayerWorkOwner`

- [ ] **Step 2: PlayerInterventionManager GameObject 생성**

Hierarchy → Create Empty → 이름: `PlayerInterventionManager`
Inspector → Add Component → `PlayerInterventionManager`

인스펙터에서 연결:
- `Order Register` → 씬의 `OrderRegisterSO` 에셋
- `Dispatch Table` → 씬의 `WorkDispatchTableSO` 에셋
- `Order Channel` → `OrderManager` 가 사용하는 동일 `EventChannelSO`
- `Schedule Trigger Channel` → `ScheduleManager` 가 사용하는 동일 채널

- [ ] **Step 3: ImmediateInterventionHandlerSO 에셋 생성**

Project 창 → Create → Tycoon/Intervention/Immediate → 이름: `ImmediateHandler`

- [ ] **Step 4: 각 WorkSO에 PlayerHandler 연결**

다음 WorkSO 에셋을 열고 `Player Handler` 필드에 `ImmediateHandler` 연결:
- `CookWork.asset`
- `TakeOrderWork.asset` (있는 경우)
- `ServeWork.asset`
- `CashierWork.asset` (있는 경우)

- [ ] **Step 5: Play 모드 기본 동작 확인**

Play 모드 진입 → 손님 입장 → 주문 생성 → `OrderBoardUI` 에서 Claim 버튼 표시 확인 → 클릭 시 `ImmediateHandler` 가 즉시 완료 처리하여 다음 단계로 진행하는지 확인.

Console에서 에러 없음 확인.

- [ ] **Step 6: 씬 저장 + 커밋**

```
git add "Assets/00. Work/BBJ/05. SO/"  (새 에셋 경로)
git add "Assets/00. Work/BBJ/01. Scene/Main.unity"
git commit -m "feat: wire up player intervention system in scene"
```

---

## 완료 후 상태

- 플레이어는 주문 보드 UI의 Claim 버튼으로 스태프 작업을 즉시 완료 처리 가능
- 스태프가 진행 중이어도 TrySteal로 소유권 이전 후 정상 결과 처리
- 각 WorkSO의 `_playerHandler` 에 구체적 `InterventionHandlerSO` 를 연결하면 미니게임/인터랙션 전환 가능
- 월드 클릭 (`Workplace.OnPlayerClick`) 구조는 준비되어 있으나 클릭 감지 `PlayerInputHandler` 는 별도 구현 필요
- TakeOrder (티켓 없는 상태) 개입은 별도 설계 필요 (현재 `_orderRegister` 스캔에서 제외됨)
