# AgentStatusUI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `CustomerWaitUI`와 `WorkIconUI`를 `AgentStatusUI` 하나로 통합하여 Customer/Staff 모두 아이콘·텍스트를 표시할 수 있도록 한다.

**Architecture:** `AgentStatusUI`는 `IAgentUI`를 구현하는 단일 MonoBehaviour로, `Image`와 `TMP_Text`를 가진다. State가 `Enter()`에서 `SetIcon()`/`SetText()`를 호출하고, `AgentUIModule`이 `IAgentUI` 자동 수집으로 관리한다.

**Tech Stack:** Unity UGUI (Image, TMP_Text), C#, namespace BBJ.UI / BBJ.States

---

## 파일 구조

| 작업 | 경로 |
|------|------|
| CREATE | `Assets/00. Work/BBJ/02. Scripts/UI/AgentStatusUI.cs` |
| MODIFY | `Assets/00. Work/BBJ/02. Scripts/Agent/States/CustomerIdleState.cs` |
| MODIFY | `Assets/00. Work/BBJ/02. Scripts/Agent/States/StaffWorkState.cs` |
| DELETE | `Assets/00. Work/BBJ/02. Scripts/UI/CustomerWaitUI.cs` |
| DELETE | `Assets/00. Work/BBJ/02. Scripts/UI/WorkIconUI.cs` |

---

## Task 1: `AgentStatusUI` 클래스 생성

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/UI/AgentStatusUI.cs`

- [ ] **Step 1: 파일 생성**

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI
{
    public class AgentStatusUI : MonoBehaviour, IAgentUI
    {
        [SerializeField] private Image    _icon;
        [SerializeField] private TMP_Text _label;

        private void Awake() => gameObject.SetActive(false);

        public void Open()  => gameObject.SetActive(true);
        public void Close() => gameObject.SetActive(false);

        public void SetIcon(Sprite sprite)
        {
            if (_icon == null) return;
            _icon.sprite = sprite;
            _icon.gameObject.SetActive(sprite != null);
        }

        public void SetText(string text)
        {
            if (_label == null) return;
            _label.text = text;
            _label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity Editor 콘솔에서 에러 없이 컴파일 완료 확인 (`read_console` 또는 Unity 창 확인).

---

## Task 2: `CustomerIdleState` 수정

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Agent/States/CustomerIdleState.cs`

현재 코드:
```csharp
private readonly IAgentUIModule _uiModule;
private readonly CustomerAgent  _customer;
// Enter():
_uiModule.SetActiveUI<CustomerWaitUI>(true);
_uiModule.Get<CustomerWaitUI>()?.Refresh(_customer);
// Exit():
_uiModule.SetActiveUI<CustomerWaitUI>(false);
```

- [ ] **Step 1: 필드 및 생성자 수정**

`CustomerWaitUI` → `AgentStatusUI`로 교체하고, `_statusUI` 캐싱 추가.

```csharp
using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.Movement;
using BBJ.Schedule;
using BBJ.UI;

namespace BBJ.States
{
    public class CustomerIdleState : TransitionAgentState
    {
        private readonly IPathMovement  _movement;
        private readonly ISchedulable   _scheduling;
        private readonly IAgentUIModule _uiModule;
        private readonly WorkAction     _workAction;
        private readonly CustomerAgent  _customer;
        private readonly AgentStatusUI  _statusUI;

        private bool _isMoveStarted;
        private bool _shouldWork;

        public CustomerIdleState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _customer = owner as CustomerAgent;

            _movement   = owner.GetModule<IPathMovement>();
            _scheduling = owner.GetModule<ISchedulable>();
            _uiModule   = owner.GetModule<IAgentUIModule>();
            _workAction = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();
            _statusUI   = _uiModule.Get<AgentStatusUI>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _isMoveStarted, CustomerState.Move);
            AddTransitionToEnum(() => _shouldWork,    CustomerState.Work);
        }

        public override void Enter()
        {
            base.Enter();
            _isMoveStarted = false;
            _shouldWork    = false;

            if (IsWorking()) { HandleWorkPhaseStarted(); return; }
            if (IsMoving())  { HandleMoveStarted();      return; }

            _statusUI?.SetText(GetWaitText());
            _uiModule.SetActiveUI<AgentStatusUI>(true);

            _movement.OnMoveStarted        += HandleMoveStarted;
            _workAction.OnWorkPhaseStarted += HandleWorkPhaseStarted;
        }

        public override void Exit()
        {
            base.Exit();
            _uiModule.SetActiveUI<AgentStatusUI>(false);

            _movement.OnMoveStarted        -= HandleMoveStarted;
            _workAction.OnWorkPhaseStarted -= HandleWorkPhaseStarted;
        }

        private void HandleMoveStarted()      => _isMoveStarted = true;
        private void HandleWorkPhaseStarted() => _shouldWork = true;
        private bool IsMoving()  => _movement != null && _movement.IsMoving;
        private bool IsWorking() => _scheduling != null && !_scheduling.IsAvailableForWork
                                    && _workAction != null && _workAction.IsInWorkPhase;

        private string GetWaitText()
        {
            if (_customer == null) return "대기";
            if (_customer.IsAwaitingOrder) return "주문 대기";
            if (_customer.OrderPlaced && !_customer.FoodServed) return "음식 대기";
            return "대기";
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity 콘솔에서 에러 없이 컴파일 완료 확인.

---

## Task 3: `StaffWorkState` 수정

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Agent/States/StaffWorkState.cs`

현재 코드:
```csharp
private readonly IAgentUIModule _uiModule;
// Enter():
_uiModule.SetActiveUI<WorkIconUI>(true);
// Exit():
_uiModule.SetActiveUI<WorkIconUI>(false);
```

- [ ] **Step 1: 필드 및 생성자 수정**

`WorkIconUI` → `AgentStatusUI`로 교체.

```csharp
using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Modules;
using BBJ.UI;

namespace BBJ.States
{
    public class StaffWorkState : TransitionAgentState
    {
        private readonly WorkAction     _workAction;
        private readonly IAgentInput    _input;
        private readonly IAgentUIModule _uiModule;
        private readonly AgentStatusUI  _statusUI;

        private bool _workEnded;
        private bool _shouldInteract;

        public StaffWorkState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _workAction = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();
            _input      = owner.GetModule<IAgentInput>();
            _uiModule   = owner.GetModule<IAgentUIModule>();
            _statusUI   = _uiModule.Get<AgentStatusUI>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _workEnded,      StaffState.Idle);
            AddTransitionToEnum(() => _shouldInteract, StaffState.Interact);
        }

        public override void Enter()
        {
            base.Enter();
            _workEnded      = false;
            _shouldInteract = false;

            _statusUI?.SetText("작업중");
            _uiModule.SetActiveUI<AgentStatusUI>(true);
            _workAction.OnWorkPhaseEnded += HandleWorkEnded;
            _input.OnInteracted          += HandleInteract;
        }

        public override void Exit()
        {
            _uiModule.SetActiveUI<AgentStatusUI>(false);
            _workAction.OnWorkPhaseEnded -= HandleWorkEnded;
            _input.OnInteracted          -= HandleInteract;
        }

        private void HandleWorkEnded() => _workEnded = true;
        private void HandleInteract()  => _shouldInteract = true;
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity 콘솔에서 에러 없이 컴파일 완료 확인.

---

## Task 4: 구 파일 삭제

**Files:**
- Delete: `Assets/00. Work/BBJ/02. Scripts/UI/CustomerWaitUI.cs`
- Delete: `Assets/00. Work/BBJ/02. Scripts/UI/CustomerWaitUI.cs.meta`
- Delete: `Assets/00. Work/BBJ/02. Scripts/UI/WorkIconUI.cs`
- Delete: `Assets/00. Work/BBJ/02. Scripts/UI/WorkIconUI.cs.meta`

- [ ] **Step 1: 두 파일 삭제**

Unity Project 창에서 삭제하거나 파일시스템에서 직접 삭제. `.meta` 파일도 함께 삭제.

- [ ] **Step 2: 컴파일 확인**

삭제 후 Unity 콘솔에 컴파일 에러 없음 확인. `CustomerWaitUI` / `WorkIconUI`를 참조하는 남은 코드가 있다면 이 시점에서 에러로 드러남.

---

## Task 5: 프리팹 / 씬 인스펙터 연결

**Files:**
- Customer Agent 프리팹 (경로는 프로젝트 내 Customer 프리팹 위치 확인)
- Staff Agent 프리팹 (경로는 프로젝트 내 Staff 프리팹 위치 확인)

- [ ] **Step 1: Customer Agent 프리팹 수정**

1. Customer Agent 프리팹 열기
2. UI 하위 오브젝트에서 `CustomerWaitUI` 컴포넌트 제거
3. 동일 오브젝트(또는 새 자식)에 `AgentStatusUI` 컴포넌트 추가
4. `_icon` 슬롯 → Image 오브젝트 연결 (없으면 자식 Image 오브젝트 생성)
5. `_label` 슬롯 → TMP_Text 오브젝트 연결 (없으면 자식 TMP_Text 오브젝트 생성)
6. 프리팹 저장

- [ ] **Step 2: Staff Agent 프리팹 수정**

1. Staff Agent 프리팹 열기
2. UI 하위 오브젝트에서 `WorkIconUI` 컴포넌트 제거
3. 동일 오브젝트(또는 새 자식)에 `AgentStatusUI` 컴포넌트 추가
4. `_icon`, `_label` 슬롯 연결
5. 프리팹 저장

- [ ] **Step 3: 씬 인스턴스 확인**

Main 씬 열고, Customer / Staff 인스턴스에 누락된 레퍼런스(Missing)가 없는지 인스펙터에서 확인.

---

## Task 6: 런타임 검증

- [ ] **Step 1: Play Mode 진입**

Main 씬에서 Play Mode 시작.

- [ ] **Step 2: Customer 대기 상태 확인**

Customer가 Idle 상태 진입 시:
- `AgentStatusUI`가 열림 (활성화)
- 상황에 맞는 텍스트 표시 (`주문 대기` 또는 `음식 대기` 또는 `대기`)
- Customer가 이동 시작 시 `AgentStatusUI` 닫힘

- [ ] **Step 3: Staff 작업 상태 확인**

Staff가 Work 상태 진입 시:
- `AgentStatusUI`가 열림
- `"작업중"` 텍스트 표시
- 작업 종료 시 `AgentStatusUI` 닫힘

- [ ] **Step 4: 콘솔 에러 없음 확인**

Play Mode 중 콘솔에 에러 / 경고 없음 확인.
