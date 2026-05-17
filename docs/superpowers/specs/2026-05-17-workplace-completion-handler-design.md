# Workplace Completion Handler Design

Date: 2026-05-17  
Branch: BBJ

## Overview

WorkModule이 작업 완료 후 실행할 핸들러 리스트를 소유한다. 각 핸들러는 추상 SO로 정의되며, 워크플레이스 프리팹 인스펙터에서 구성한다. 계산대(Cashier)는 코인 지급과 파티클 소환을 담당하는 구체 핸들러를 가진다.

---

## Goals

- WorkModule 완료 후 워크플레이스별 부작용(코인, 파티클 등)을 SO 리스트로 구성 가능하게 한다.
- 팁 계산 로직을 별도 추상 SO로 분리하여 교체 가능하게 한다.
- `IWorkExecutor` 시그니처를 변경하지 않는다.
- `ICurrentFoodProvider` / `IStatModule` 기존 패턴을 그대로 따른다.

---

## New Types

### `WorkCompletionHandlerSO` (abstract SO)

```csharp
namespace BBJ.WorkplaceSystem
{
    public abstract class WorkCompletionHandlerSO : ScriptableObject
    {
        public abstract void OnCompleted(ModuleOwner executor);
    }
}
```

- 위치: `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/`
- executor에서 필요한 정보를 `GetModule<T>()`로 직접 읽는다.

---

### `TipCalculatorSO` (abstract SO)

```csharp
namespace BBJ.WorkplaceSystem
{
    public abstract class TipCalculatorSO : ScriptableObject
    {
        public abstract int Calculate(ModuleOwner executor);
    }
}
```

- 위치: `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/`

---

### `StatTipCalculatorSO` (concrete SO)

```csharp
[CreateAssetMenu(menuName = "Tycoon/Tip/StatTip")]
public class StatTipCalculatorSO : TipCalculatorSO
{
    [SerializeField] private StatSO _stat;

    public override int Calculate(ModuleOwner executor)
    {
        var statModule = executor.GetModule<IStatModule>();
        return statModule != null && statModule.TryGetStat(_stat.AssetIndex, out var stat)
            ? Mathf.RoundToInt(stat.Value) : 0;
    }
}
```

- executor의 `IStatModule`에서 지정 스탯 값을 팁으로 반환한다.

---

### `CashierCompletionHandlerSO` (concrete SO)

```csharp
[CreateAssetMenu(menuName = "Tycoon/WorkCompletion/Cashier")]
public class CashierCompletionHandlerSO : WorkCompletionHandlerSO
{
    [SerializeField] private float            _stageMultiplier = 1f;
    [SerializeField] private TipCalculatorSO  _tipCalculator;
    [SerializeField] private EventChannelSO   _coinChannel;
    [SerializeField] private EventChannelSO   _particleChannel;
    [SerializeField] private CostParticleType _particleType;

    public override void OnCompleted(ModuleOwner executor)
    {
        var food = executor.GetModule<ICurrentFoodProvider>()?.CurrentFood;
        if (food == null) return;

        int tip    = _tipCalculator != null ? _tipCalculator.Calculate(executor) : 0;
        int amount = Mathf.RoundToInt(food.Price * _stageMultiplier) + tip;

        _coinChannel?.RaiseEvent(new CoinEvent().Init(amount));
        _particleChannel?.RaiseEvent(
            new CostParticleEvent().Init(_particleType, amount, executor.transform.position));
    }
}
```

**코인 공식:** `amount = floor(food.Price * _stageMultiplier) + tip`  
**_stageMultiplier:** 스테이지 시스템이 생기면 SO 교체 또는 외부 주입으로 대체한다.

---

## Modified Types

### `WorkModule`

`_completionHandlers` 리스트 추가, `ExecuteWorkAsync` 루프 완료 후 호출.

```csharp
[SerializeField] private List<WorkCompletionHandlerSO> _completionHandlers = new();

public async UniTask ExecuteWorkAsync(ModuleOwner worker, CancellationToken ct)
{
    float duration = GetDuration(worker);
    float elapsed  = 0f;
    while (elapsed < duration)
    {
        await UniTask.WaitForFixedUpdate(cancellationToken: ct);
        elapsed += Time.fixedDeltaTime;
        OnProgressChanged?.Invoke(elapsed / duration);
    }
    foreach (var handler in _completionHandlers)
        handler.OnCompleted(worker);
}
```

- 취소(`OperationCanceledException`) 시 루프 탈출이 일어나지 않으므로 핸들러는 호출되지 않는다.

---

### `CashierWorkSO`

WorkAction 실행 전, foodContext에 음식 정보를 설정하는 한 줄 추가.

```csharp
ticket.TryStartProgress(executor);
foodContext?.SetFood(ticket.Food);   // 추가

_ctx.OrderChannel?.RaiseEvent(new CookingStartEvent(ticket, executor));
await actions.Execute<WorkAction>(a => a.ExecuteAsync(counter, linked.Token));
```

---

## Call Flow

```
CashierWorkSO.RunAsync()
  └─ foodContext.SetFood(ticket.Food)
  └─ actions.Execute<WorkAction>(counter, token)
       └─ WorkAction.ExecuteAsync()
            └─ WorkModule.ExecuteWorkAsync(worker, ct)
                 └─ [루프 완료]
                 └─ foreach handler.OnCompleted(worker)
                      └─ CashierCompletionHandlerSO
                           ├─ ICurrentFoodProvider → food.Price  (x)
                           ├─ TipCalculatorSO.Calculate()        (tip)
                           ├─ amount = (x * n) + tip
                           ├─ CoinEvent → CoinManager
                           └─ CostParticleEvent → CostParticleManager
```

---

## File Locations

| Type | Path |
|------|------|
| `WorkCompletionHandlerSO` | `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/WorkCompletionHandlerSO.cs` |
| `TipCalculatorSO` | `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/TipCalculatorSO.cs` |
| `StatTipCalculatorSO` | `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/StatTipCalculatorSO.cs` |
| `CashierCompletionHandlerSO` | `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/CashierCompletionHandlerSO.cs` |
| `WorkModule` (수정) | `Assets/00. Work/BBJ/02. Scripts/Workplace/Modules/WorkModule.cs` |
| `CashierWorkSO` (수정) | `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs` |

---

## Out of Scope

- 스테이지 시스템 연동 (`_stageMultiplier`는 임시 float)
- Kitchen/Serve 등 다른 워크플레이스용 핸들러 (같은 패턴으로 추가 가능)
- 코인 외 다른 보상 타입
