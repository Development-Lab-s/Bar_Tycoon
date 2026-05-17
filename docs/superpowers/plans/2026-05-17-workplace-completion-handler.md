# Workplace Completion Handler Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** WorkModule 완료 후 SO 리스트를 통해 워크플레이스별 부작용(코인 지급, 파티클 소환)을 실행하는 시스템을 구축한다.

**Architecture:** `WorkCompletionHandlerSO` 추상 SO를 기반으로 `WorkModule`이 진행 루프 완료 후 직렬로 핸들러를 호출한다. `CashierCompletionHandlerSO`는 executor의 `ICurrentFoodProvider`로 가격을, `IStatModule`로 팁을 읽어 코인·파티클 이벤트를 발행한다. `CashierWorkSO`는 WorkAction 실행 전 `FoodContextModule.SetFood()`를 호출해 핸들러가 음식 정보에 접근할 수 있도록 한다. 취소(OperationCanceledException) 시에는 루프를 탈출하므로 핸들러는 자동으로 호출되지 않는다.

**Tech Stack:** Unity 2D, C#, UniTask, ScriptableObject event channels (`EventChannelSO`), `ICurrentFoodProvider`, `IStatModule`

---

## File Map

| 상태 | 파일 | 역할 |
|------|------|------|
| Create | `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/WorkCompletionHandlerSO.cs` | 핸들러 추상 기반 |
| Create | `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/TipCalculatorSO.cs` | 팁 계산 추상 기반 |
| Create | `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/StatTipCalculatorSO.cs` | 스탯 기반 팁 계산 구체 |
| Create | `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/CashierCompletionHandlerSO.cs` | 계산대 완료 핸들러 구체 |
| Modify | `Assets/00. Work/BBJ/02. Scripts/Workplace/Modules/WorkModule.cs` | 핸들러 리스트 추가 및 완료 후 호출 |
| Modify | `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs` | WorkAction 전 foodContext.SetFood 추가 |

---

## Task 1: 추상 기반 SO 두 개 생성

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/WorkCompletionHandlerSO.cs`
- Create: `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/TipCalculatorSO.cs`

- [ ] **Step 1: WorkCompletionHandlerSO 생성**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/WorkCompletionHandlerSO.cs
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem.Handlers
{
    public abstract class WorkCompletionHandlerSO : ScriptableObject
    {
        public abstract void OnCompleted(ModuleOwner executor);
    }
}
```

- [ ] **Step 2: TipCalculatorSO 생성**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/TipCalculatorSO.cs
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem.Handlers
{
    public abstract class TipCalculatorSO : ScriptableObject
    {
        public abstract int Calculate(ModuleOwner executor);
    }
}
```

- [ ] **Step 3: 컴파일 확인**

UnityMCP `read_console`로 에러 없음 확인.

- [ ] **Step 4: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/WorkCompletionHandlerSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/TipCalculatorSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/WorkCompletionHandlerSO.cs.meta"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/TipCalculatorSO.cs.meta"
git commit -m "feat: add WorkCompletionHandlerSO and TipCalculatorSO abstract bases"
```

---

## Task 2: StatTipCalculatorSO 구체 구현

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/StatTipCalculatorSO.cs`

- [ ] **Step 1: StatTipCalculatorSO 생성**

executor의 `IStatModule`에서 지정한 StatSO의 Value를 int로 반올림해 반환한다. `IStatModule`을 가진 모듈이 없거나 stat을 찾지 못하면 0을 반환한다.

```csharp
// Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/StatTipCalculatorSO.cs
using Agents.StatSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem.Handlers
{
    [CreateAssetMenu(fileName = "StatTipCalculator", menuName = "Tycoon/Tip/StatTip")]
    public class StatTipCalculatorSO : TipCalculatorSO
    {
        [SerializeField] private StatSO _stat;

        public override int Calculate(ModuleOwner executor)
        {
            var statModule = executor.GetModule<IStatModule>();
            if (statModule == null) return 0;
            return statModule.TryGetStat(_stat.AssetIndex, out StatSO stat)
                ? Mathf.RoundToInt(stat.Value) : 0;
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

UnityMCP `read_console`로 에러 없음 확인.

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/StatTipCalculatorSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/StatTipCalculatorSO.cs.meta"
git commit -m "feat: add StatTipCalculatorSO concrete tip calculator"
```

---

## Task 3: CashierCompletionHandlerSO 구체 구현

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/CashierCompletionHandlerSO.cs`

- [ ] **Step 1: CashierCompletionHandlerSO 생성**

코인 공식: `amount = Mathf.RoundToInt(food.Price * _stageMultiplier) + tip`  
`food`가 null이면 아무것도 하지 않고 리턴한다.

```csharp
// Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/CashierCompletionHandlerSO.cs
using _00._Work.Goat._02._Scripts.Events;
using BBJ.Particle;
using Gamelib.EventSystem;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Work;

namespace BBJ.WorkplaceSystem.Handlers
{
    [CreateAssetMenu(fileName = "CashierCompletion", menuName = "Tycoon/WorkCompletion/Cashier")]
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
}
```

- [ ] **Step 2: 컴파일 확인**

UnityMCP `read_console`로 에러 없음 확인.

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/CashierCompletionHandlerSO.cs"
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Handlers/CashierCompletionHandlerSO.cs.meta"
git commit -m "feat: add CashierCompletionHandlerSO with coin and particle events"
```

---

## Task 4: WorkModule에 핸들러 리스트 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Workplace/Modules/WorkModule.cs`

현재 `ExecuteWorkAsync`의 루프 완료 직후, `_completionHandlers` 리스트의 각 핸들러를 순서대로 호출한다. 취소 시에는 루프가 `OperationCanceledException`을 throw하므로 핸들러가 호출되지 않는다.

- [ ] **Step 1: WorkModule 수정**

```csharp
// Assets/00. Work/BBJ/02. Scripts/Workplace/Modules/WorkModule.cs
using BBJ.Work;
using BBJ.WorkplaceSystem.Handlers;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem.Modules
{
    public class WorkModule : MonoBehaviour, IModule, IWorkExecutor
    {
        [SerializeField] private float _fallbackDuration = 1f;
        [SerializeField] private WorkDurationSO _durationSO;
        [SerializeField] private List<WorkCompletionHandlerSO> _completionHandlers = new();

        public event Action<float> OnProgressChanged;

        public void Initialize(ModuleOwner owner) { }

        public float GetDuration(ModuleOwner worker)
        {
            if (_durationSO == null) return _fallbackDuration;
            return _durationSO.GetDuration(worker);
        }

        public IEnumerator ExecuteWork(ModuleOwner worker)
        {
            yield return new WaitForSeconds(GetDuration(worker));
        }

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
    }
}
```

- [ ] **Step 2: 컴파일 확인**

UnityMCP `read_console`로 에러 없음 확인.

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Workplace/Modules/WorkModule.cs"
git commit -m "feat: add completion handler list to WorkModule"
```

---

## Task 5: CashierWorkSO에 foodContext.SetFood 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs`

`ticket.TryStartProgress()` 직후, `WorkAction` 실행 전에 `foodContext?.SetFood(ticket.Food)`를 추가한다. 이 줄이 없으면 `CashierCompletionHandlerSO`가 `ICurrentFoodProvider`로 food를 읽을 수 없다.

- [ ] **Step 1: CashierWorkSO 수정**

`FoodContextModule`은 `BBJ.Modules` namespace이며 `CashierWorkSO`에 이미 `using BBJ.Modules;`가 있다. `ticket.TryStartProgress(executor);` 다음 줄에 두 줄을 추가한다.

```csharp
// 기존
ticket.TryStartProgress(executor);
await actions.Execute<WaitAction>(
    a => a.ExecuteAsync(() => queue.HasWaiting, linked.Token));
```

```csharp
// 변경 후
ticket.TryStartProgress(executor);
var foodContext = executor.GetModule<FoodContextModule>();
foodContext?.SetFood(ticket.Food);
await actions.Execute<WaitAction>(
    a => a.ExecuteAsync(() => queue.HasWaiting, linked.Token));
```

- [ ] **Step 2: 컴파일 확인**

UnityMCP `read_console`로 에러 없음 확인.

- [ ] **Step 3: 커밋**

```bash
git add "Assets/00. Work/BBJ/02. Scripts/Work/CashierWorkSO.cs"
git commit -m "feat: set food context before WorkAction in CashierWorkSO"
```

---

## Task 6: SO 에셋 생성 및 인스펙터 연결

이 태스크는 Unity 에디터에서 직접 수행한다.

- [ ] **Step 1: StatTipCalculator SO 에셋 생성**

Project 창에서 `Assets/00. Work/BBJ/05. SO/` (또는 기존 SO 에셋 위치) 우클릭 →  
`Create > Tycoon > Tip > StatTip` → 이름: `StatTipCalculator`  
인스펙터에서 `_stat` 필드에 팁에 사용할 StatSO 에셋 연결.

- [ ] **Step 2: CashierCompletion SO 에셋 생성**

`Create > Tycoon > WorkCompletion > Cashier` → 이름: `CashierCompletion`  
인스펙터 설정:
- `_stageMultiplier`: `1` (임시)
- `_tipCalculator`: Step 1에서 만든 `StatTipCalculator` 에셋
- `_coinChannel`: 기존 코인 이벤트 채널 SO (CoinManager가 구독하는 것)
- `_particleChannel`: `CostParticleConfigSO._config.particleChannel`과 동일한 채널 SO
- `_particleType`: 원하는 `CostParticleType` 값

- [ ] **Step 3: Counter Workplace 프리팹에 핸들러 연결**

Counter Workplace 프리팹의 `WorkModule` 컴포넌트 →  
`Completion Handlers` 리스트에 Step 2에서 만든 `CashierCompletion` 에셋 추가.

- [ ] **Step 4: 인에디터 검증**

1. Play 모드 진입
2. 손님이 주문 → 직원이 조리 → 손님이 카운터 줄 섬 → 직원(Cashier)이 계산 진행
3. 직원의 Work 진행 바가 끝나는 순간:
   - 코인이 `food.Price × 1 + tipStat` 만큼 증가하는지 확인
   - CostParticle이 직원 위치에 소환되는지 확인
4. Console에 에러 없음 확인

- [ ] **Step 5: 커밋**

```bash
git add "Assets/00. Work/BBJ/05. SO/"  # SO 에셋 파일들
git commit -m "feat: wire up CashierCompletion SO assets to Counter workplace"
```

---

## 검증 체크리스트

| 항목 | 확인 방법 |
|------|-----------|
| 취소 시 핸들러 미호출 | Play 중 직원이 이동 중에 씬을 바꾸거나 취소 — 코인 증가 없음 확인 |
| food null 시 방어 | foodContext.SetFood가 호출되지 않은 상태에서 OnCompleted → 코인 증가 없음 |
| 핸들러 리스트 비어 있을 때 | 리스트 비운 뒤 플레이 — NullReference 없음 확인 |
| 스테이지 배율 변경 | `_stageMultiplier=2` 설정 후 price=1000이면 코인 2000+tip 확인 |
