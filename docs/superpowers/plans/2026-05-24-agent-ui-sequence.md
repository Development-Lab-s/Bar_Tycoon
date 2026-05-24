# Agent UI Sequence System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Customer가 주문 대기 시 대사 말풍선을 표시한 뒤 메뉴 UI로 전환하는 시퀀스 시스템과 먹기 진행 DurationUI를 구현한다.

**Architecture:** `IAgentUI`를 `OpenAsync/CloseAsync` 기반으로 전면 교체하고, `AgentUIModule`에 `PlaySequenceAsync`를 추가해 UI 시퀀스를 구동한다. `CharacterDataSO`에 대사 풀을 추가하고, `CustomerIdleState`에서 시퀀스를 시작한다.

**Tech Stack:** Unity 2D, UniTask, LitMotion, TextMeshPro, UGUI

---

## 파일 맵

| 파일 | 작업 |
|---|---|
| `Assets/00. Work/BBJ/02. Scripts/UI/IAgentUI.cs` | 전면 교체 |
| `Assets/00. Work/BBJ/02. Scripts/Modules/IAgentUIModule.cs` | SetActiveUI 제거, PlaySequenceAsync/CancelSequence 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Modules/AgentUIModule.cs` | 시퀀스 엔진 구현 |
| `Assets/00. Work/BBJ/02. Scripts/UI/AgentStatusUI.cs` | async LitMotion 패턴으로 교체 |
| `Assets/00. Work/BBJ/02. Scripts/UI/DialogueBubbleUI.cs` | 신규 생성 |
| `Assets/00. Work/BBJ/02. Scripts/UI/EatDurationUI.cs` | 신규 생성 |
| `Assets/00. Work/PTY/02. Scripts/CharacterDataSO.cs` | DialogueLine 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs` | CharacterData 필드 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Agent/Actions/WorkAction.cs` | OnProgressUpdated 이벤트 추가 |
| `Assets/00. Work/BBJ/02. Scripts/Agent/States/CustomerIdleState.cs` | PlayOrderSequenceAsync 구현 |
| `Assets/00. Work/BBJ/02. Scripts/Agent/States/CustomerWorkState.cs` | EatDurationUI 연결 |

---

## Task 1: IAgentUI + IAgentUIModule 인터페이스 교체

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/UI/IAgentUI.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Modules/IAgentUIModule.cs`

- [ ] **Step 1: IAgentUI 전면 교체**

`Assets/00. Work/BBJ/02. Scripts/UI/IAgentUI.cs` 전체를 다음으로 교체:

```csharp
using Cysharp.Threading.Tasks;

namespace BBJ.UI
{
    public interface IAgentUI
    {
        bool IsOpen { get; }
        UniTask OpenAsync();
        UniTask CloseAsync();
    }
}
```

- [ ] **Step 2: IAgentUIModule 교체**

`Assets/00. Work/BBJ/02. Scripts/Modules/IAgentUIModule.cs` 전체를 다음으로 교체:

```csharp
using System.Threading;
using BBJ.UI;
using Cysharp.Threading.Tasks;

namespace BBJ.Modules
{
    public interface IAgentUIModule
    {
        T Get<T>() where T : class, IAgentUI;
        UniTask PlaySequenceAsync(CancellationToken ct, params IAgentUI[] sequence);
        void CancelSequence();
        void CloseAll();
    }
}
```

- [ ] **Step 3: 컴파일 오류 확인**

Unity Editor Console에서 컴파일 오류 목록 확인. `IAgentUI` 또는 `IAgentUIModule`의 기존 구현체(`AgentUIModule`, `AgentStatusUI`)에서 오류가 발생하는 것이 정상 — 이후 Task에서 수정한다.

---

## Task 2: AgentUIModule — 시퀀스 엔진

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Modules/AgentUIModule.cs`

- [ ] **Step 1: AgentUIModule 전체 교체**

```csharp
using BBJ.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Modules
{
    public class AgentUIModule : MonoBehaviour, IModule, IAgentUIModule
    {
        private Dictionary<Type, IAgentUI> _uis;
        private CancellationTokenSource    _sequenceCts;

        public void Initialize(ModuleOwner owner)
        {
            _uis = GetComponentsInChildren<IAgentUI>(true)
                .ToDictionary(ui => ui.GetType());
        }

        public T Get<T>() where T : class, IAgentUI
        {
            _uis.TryGetValue(typeof(T), out var ui);
            return ui as T;
        }

        public async UniTask PlaySequenceAsync(CancellationToken ct, params IAgentUI[] sequence)
        {
            CancelSequence();
            _sequenceCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = _sequenceCts.Token;

            for (int i = 0; i < sequence.Length; i++)
            {
                if (token.IsCancellationRequested) break;
                await sequence[i].OpenAsync().AttachExternalCancellation(token);

                bool isLast = i == sequence.Length - 1;
                if (!isLast)
                    await sequence[i].CloseAsync().AttachExternalCancellation(token);
            }
        }

        public void CancelSequence()
        {
            _sequenceCts?.Cancel();
            _sequenceCts?.Dispose();
            _sequenceCts = null;
        }

        public void CloseAll()
        {
            CancelSequence();
            foreach (var ui in _uis.Values)
                _ = ui.CloseAsync();
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Console에서 `AgentUIModule` 관련 오류 없는지 확인.

---

## Task 3: AgentStatusUI — async LitMotion 패턴

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/UI/AgentStatusUI.cs`

- [ ] **Step 1: AgentStatusUI 전체 교체**

```csharp
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI
{
    public class AgentStatusUI : MonoBehaviour, IAgentUI
    {
        [SerializeField] private Image      _icon;
        [SerializeField] private TMP_Text   _label;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float      _animDuration = 0.15f;

        public bool IsOpen { get; private set; }

        private void Start()
        {
            gameObject.SetActive(false);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        public async UniTask OpenAsync()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            if (_canvasGroup == null) return;
            await LMotion.Create(0f, 1f, _animDuration)
                .WithEase(Ease.OutCubic)
                .BindToCanvasGroupAlpha(_canvasGroup)
                .AddTo(this);
        }

        public async UniTask CloseAsync()
        {
            if (_canvasGroup != null)
                await LMotion.Create(1f, 0f, _animDuration)
                    .WithEase(Ease.InCubic)
                    .BindToCanvasGroupAlpha(_canvasGroup)
                    .AddTo(this);
            gameObject.SetActive(false);
            IsOpen = false;
        }

        public void SetIcon(Sprite sprite)
        {
            if (_icon == null) return;
            _icon.sprite = sprite;
            _icon.color  = Color.white;
            _icon.gameObject.SetActive(true);
            _label.gameObject.SetActive(false);
        }

        public void SetIconColor(Color color)
        {
            if (_icon == null) return;
            _icon.color = color;
        }

        public void SetText(string text)
        {
            if (_label == null) return;
            _label.text = text;
            _icon.gameObject.SetActive(false);
            _label.gameObject.SetActive(true);
        }
    }
}
```

- [ ] **Step 2: Prefab Inspector 확인**

`CustomerAgent` Prefab에서 `AgentStatusUI` 컴포넌트를 찾아 `CanvasGroup` 필드에 `CanvasGroup` 컴포넌트 할당 확인. 없으면 해당 GameObject에 `CanvasGroup` 컴포넌트 추가 후 할당.

- [ ] **Step 3: 컴파일 확인**

Console에서 오류 없는지 확인.

---

## Task 4: DialogueBubbleUI — 대사 말풍선 (신규)

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/UI/DialogueBubbleUI.cs`

- [ ] **Step 1: DialogueBubbleUI 생성**

```csharp
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using System;

namespace BBJ.UI
{
    public class DialogueBubbleUI : MonoBehaviour, IAgentUI
    {
        [SerializeField] private TMP_Text    _label;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float       _displayDuration = 1.5f;
        [SerializeField] private float       _animDuration    = 0.2f;

        public bool IsOpen { get; private set; }

        private void Start()
        {
            gameObject.SetActive(false);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        public void SetText(string text)
        {
            if (_label != null) _label.text = text;
        }

        public async UniTask OpenAsync()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            if (_canvasGroup != null)
                await LMotion.Create(0f, 1f, _animDuration)
                    .WithEase(Ease.OutCubic)
                    .BindToCanvasGroupAlpha(_canvasGroup)
                    .AddTo(this);
            await UniTask.Delay(TimeSpan.FromSeconds(_displayDuration));
        }

        public async UniTask CloseAsync()
        {
            if (_canvasGroup != null)
                await LMotion.Create(1f, 0f, _animDuration)
                    .WithEase(Ease.InCubic)
                    .BindToCanvasGroupAlpha(_canvasGroup)
                    .AddTo(this);
            gameObject.SetActive(false);
            IsOpen = false;
        }
    }
}
```

- [ ] **Step 2: Prefab에 GameObject 추가**

`CustomerAgent` Prefab의 UI 루트 하위에 `DialogueBubble` GameObject 생성:
- `CanvasGroup` 컴포넌트 추가
- `TMP_Text` 컴포넌트 추가 (말풍선 텍스트)
- `DialogueBubbleUI` 컴포넌트 추가 → `_label`, `_canvasGroup` 필드 연결
- `_displayDuration` = 1.5, `_animDuration` = 0.2 설정

- [ ] **Step 3: 컴파일 확인**

Console에서 오류 없는지 확인.

---

## Task 5: EatDurationUI — 먹기 진행 (신규)

**Files:**
- Create: `Assets/00. Work/BBJ/02. Scripts/UI/EatDurationUI.cs`

- [ ] **Step 1: EatDurationUI 생성**

```csharp
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.UI
{
    public class EatDurationUI : MonoBehaviour, IAgentUI, IModule
    {
        [SerializeField] private Slider   _slider;
        [SerializeField] private float    _animDuration = 0.15f;

        public bool IsOpen { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = 1f;
            }
            gameObject.SetActive(false);
        }

        public void SetPercent(float value)
        {
            if (_slider != null)
                _slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }

        public async UniTask OpenAsync()
        {
            SetPercent(0f);
            gameObject.SetActive(true);
            IsOpen = true;
            await LMotion.Create(Vector3.zero, Vector3.one, _animDuration)
                .WithEase(Ease.OutBack)
                .Bind(v => transform.localScale = v)
                .AddTo(this);
        }

        public async UniTask CloseAsync()
        {
            await LMotion.Create(Vector3.one, Vector3.zero, _animDuration)
                .WithEase(Ease.InCubic)
                .Bind(v => transform.localScale = v)
                .AddTo(this);
            gameObject.SetActive(false);
            IsOpen = false;
        }
    }
}
```

- [ ] **Step 2: Prefab에 GameObject 추가**

`CustomerAgent` Prefab의 UI 루트 하위에 `EatDuration` GameObject 생성:
- `Slider` 컴포넌트 추가 (또는 기존 Slider 재사용)
- `EatDurationUI` 컴포넌트 추가 → `_slider` 필드 연결
- `_animDuration` = 0.15 설정
- 초기 `localScale = (0, 0, 0)` 설정 (닫힌 상태)

- [ ] **Step 3: 컴파일 확인**

Console에서 오류 없는지 확인.

---

## Task 6: CharacterDataSO + CustomerAgent — 대사 데이터

**Files:**
- Modify: `Assets/00. Work/PTY/02. Scripts/CharacterDataSO.cs`
- Modify: `Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs`

- [ ] **Step 1: CharacterDataSO에 DialogueLine 추가**

`Assets/00. Work/PTY/02. Scripts/CharacterDataSO.cs` 전체를 다음으로 교체:

```csharp
using System;
using UnityEngine;

public enum DialogueSituation
{
    WaitingOrder,
}

[Serializable]
public struct DialogueLine
{
    public DialogueSituation Situation;
    [TextArea] public string[] Lines;
}

[CreateAssetMenu(fileName = "CharacterData", menuName = "SO/CharacterData")]
public class CharacterDataSO : ScriptableObject
{
    public string characterName;
    public Sprite characterImage;
    public Sprite icon;
    [TextArea] public string description;
    public string job;
    public string specialty;
    public string hobby;
    public string favoriteCocktail;

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] _dialogueLines;

    public string GetLine(DialogueSituation situation)
    {
        if (_dialogueLines == null) return string.Empty;
        foreach (var entry in _dialogueLines)
        {
            if (entry.Situation == situation && entry.Lines?.Length > 0)
                return entry.Lines[UnityEngine.Random.Range(0, entry.Lines.Length)];
        }
        return string.Empty;
    }
}
```

- [ ] **Step 2: CustomerAgent에 CharacterData 필드 추가**

`Assets/00. Work/BBJ/02. Scripts/Customer/CustomerAgent.cs`의 직렬화 필드 영역에 추가:

```csharp
// 기존 필드들 아래에 추가
[SerializeField] private CharacterDataSO _characterData;
public CharacterDataSO CharacterData => _characterData;
```

- [ ] **Step 3: CharacterDataSO asset에 대사 입력**

기존 `CharacterDataSO` asset들을 Inspector에서 열어 `Dialogue Lines` 배열에:
- Situation: `WaitingOrder`
- Lines: `["달달한 걸로 주세요", "[음식명] 주세요"]` 등 입력

- [ ] **Step 4: CustomerAgent Prefab에 CharacterData 연결**

`CustomerAgent` Prefab Inspector에서 `_characterData` 필드에 해당 `CharacterDataSO` asset 할당.

- [ ] **Step 5: 컴파일 확인**

Console에서 오류 없는지 확인.

---

## Task 7: WorkAction — OnProgressUpdated 이벤트 추가

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Agent/Actions/WorkAction.cs`

- [ ] **Step 1: WorkAction 읽기**

`Assets/00. Work/BBJ/02. Scripts/Agent/Actions/WorkAction.cs` 파일을 읽어 현재 구조 파악.

- [ ] **Step 2: OnProgressUpdated 이벤트 추가**

`WorkAction` 클래스에 다음을 추가:

```csharp
public event Action<float> OnProgressUpdated;
```

`ExecuteAsync` 내부에서 `workExecutor.OnProgressChanged`를 구독할 때 함께 전파:

```csharp
// 기존
workExecutor.OnProgressChanged += _durationUI.SetPercent;

// 변경 — 람다로 양쪽 전파
void HandleProgress(float p)
{
    _durationUI?.SetPercent(p);
    OnProgressUpdated?.Invoke(p);
}
workExecutor.OnProgressChanged += HandleProgress;
// ... finally 블록에서
workExecutor.OnProgressChanged -= HandleProgress;
```

- [ ] **Step 3: 컴파일 확인**

Console에서 오류 없는지 확인.

---

## Task 8: CustomerIdleState — 시퀀스 통합

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Agent/States/CustomerIdleState.cs`

- [ ] **Step 1: CustomerIdleState 전체 교체**

```csharp
using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.Movement;
using BBJ.Schedule;
using BBJ.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace BBJ.States
{
    public class CustomerIdleState : CustomerAgentState
    {
        private readonly IPathMovement  _movement;
        private readonly ISchedulable   _scheduling;
        private readonly IAgentUIModule _uiModule;
        private readonly WorkAction     _workAction;
        private readonly AgentStatusUI  _statusUI;
        private readonly DialogueBubbleUI _dialogueUI;

        private bool _isMoveStarted;
        private bool _shouldWork;
        private CancellationTokenSource _uiCts;

        public CustomerIdleState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _movement   = owner.GetModule<IPathMovement>();
            _scheduling = owner.GetModule<ISchedulable>();
            _uiModule   = owner.GetModule<IAgentUIModule>();
            _workAction = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();
            _statusUI   = _uiModule.Get<AgentStatusUI>();
            _dialogueUI = _uiModule.Get<DialogueBubbleUI>();

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

            _uiCts = new CancellationTokenSource();
            PlayOrderSequenceAsync().Forget();

            _movement.OnMoveStarted        += HandleMoveStarted;
            _workAction.OnWorkPhaseStarted += HandleWorkPhaseStarted;
            _customer.OnOrderStateChanged  += RefreshUI;
        }

        public override void Exit()
        {
            base.Exit();
            _uiCts?.Cancel();
            _uiCts?.Dispose();
            _uiCts = null;
            _uiModule.CloseAll();

            _movement.OnMoveStarted        -= HandleMoveStarted;
            _workAction.OnWorkPhaseStarted -= HandleWorkPhaseStarted;
            _customer.OnOrderStateChanged  -= RefreshUI;
        }

        private void HandleMoveStarted()      => _isMoveStarted = true;
        private void HandleWorkPhaseStarted() => _shouldWork = true;
        private bool IsMoving()  => _movement != null && _movement.IsMoving;
        private bool IsWorking() => _scheduling != null && !_scheduling.IsAvailableForWork
                                    && _workAction != null && _workAction.IsInWorkPhase;

        private async UniTaskVoid PlayOrderSequenceAsync()
        {
            if (_uiCts == null) return;
            var ct = _uiCts.Token;

            var line = _customer.CharacterData?.GetLine(DialogueSituation.WaitingOrder);

            if (!string.IsNullOrEmpty(line) && _dialogueUI != null)
            {
                RefreshStatusUI();
                _dialogueUI.SetText(line);
                await _uiModule.PlaySequenceAsync(ct, _dialogueUI, _statusUI);
            }
            else
            {
                await _uiModule.PlaySequenceAsync(ct, _statusUI);
            }

            if (!ct.IsCancellationRequested)
                RefreshStatusUI();
        }

        private void RefreshUI() => RefreshStatusUI();

        private void RefreshStatusUI()
        {
            if (_statusUI == null) return;

            if (_customer.FoodServed)
            {
                _ = _statusUI.CloseAsync();
                return;
            }

            if (!_customer.OrderPlaced || _customer.IsAwaitingOrder)
            {
                _statusUI.SetText("...");
                return;
            }

            var ticket = _customer.ActiveTicket;
            var icon   = _customer.SelectedFood?.cocktailIcon;
            if (icon != null)
            {
                _statusUI.SetIcon(icon);
                _statusUI.SetIconColor(ticket?.IsPlayerActionable ?? false ? Color.white : Color.gray);
            }
            else
            {
                _statusUI.SetText(_customer.SelectedFood?.cocktailName ?? "...");
            }
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Console에서 오류 없는지 확인.

- [ ] **Step 3: Play Mode 검증**

1. 게임 실행 → 손님 스폰
2. 손님이 착석 후 Idle 상태 진입 시 말풍선 UI가 fade-in으로 등장
3. `_displayDuration` 후 말풍선 fade-out → 메뉴 상태 UI가 fade-in으로 등장
4. Console 오류 없는지 확인

---

## Task 9: CustomerWorkState — EatDurationUI 연결

**Files:**
- Modify: `Assets/00. Work/BBJ/02. Scripts/Agent/States/CustomerWorkState.cs`

- [ ] **Step 1: CustomerWorkState 전체 교체**

```csharp
using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Actions;
using BBJ.Customer;
using BBJ.Modules;
using BBJ.UI;
using Cysharp.Threading.Tasks;

namespace BBJ.States
{
    public class CustomerWorkState : CustomerAgentState
    {
        private readonly WorkAction     _workAction;
        private readonly IAgentUIModule _uiModule;
        private readonly EatDurationUI  _eatDurationUI;

        private bool _workEnded;

        public CustomerWorkState(Agent owner, AnimParamSO stateParam) : base(owner, stateParam)
        {
            _workAction    = owner.GetModule<IAgentActionModule>().GetAction<WorkAction>();
            _uiModule      = owner.GetModule<IAgentUIModule>();
            _eatDurationUI = _uiModule?.Get<EatDurationUI>();

            UtilDebugger.AssertAllAssigned(this);

            AddTransitionToEnum(() => _workEnded, CustomerState.Idle);
        }

        public override void Enter()
        {
            base.Enter();
            _workEnded = false;
            _workAction.OnWorkPhaseEnded   += HandleWorkEnded;
            _workAction.OnProgressUpdated  += HandleProgress;

            _ = _eatDurationUI?.OpenAsync();
        }

        public override void Exit()
        {
            _workAction.OnWorkPhaseEnded  -= HandleWorkEnded;
            _workAction.OnProgressUpdated -= HandleProgress;

            _ = _eatDurationUI?.CloseAsync();
        }

        private void HandleWorkEnded()            => _workEnded = true;
        private void HandleProgress(float value)  => _eatDurationUI?.SetPercent(value);
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Console에서 오류 없는지 확인.

- [ ] **Step 3: Play Mode 검증 — 전체 흐름**

1. 게임 실행 → 손님 스폰 → 착석 → Idle 진입
2. 말풍선 대사 표시 (CharacterDataSO에 WaitingOrder 라인 입력 필요)
3. 대사 사라진 후 메뉴 상태 UI 등장
4. 주문 처리 → 음식 서빙 → Work 상태 진입
5. EatDurationUI 슬라이더 등장 → 먹으면서 progress 증가
6. 다 먹으면 EatDurationUI 사라짐
7. 전체 흐름에서 Console 오류 없는지 확인

---

## 셀프 리뷰

**스펙 대비 커버리지:**
- [x] 대사 말풍선 UI (DialogueBubbleUI) — Task 4
- [x] 상황별 대사 (CharacterDataSO) — Task 6
- [x] 시퀀스: 대사→메뉴 UI (PlaySequenceAsync) — Task 2, 8
- [x] LitMotion 애니메이션 — Task 3, 4, 5
- [x] CloseAll / CancelSequence — Task 2
- [x] IAgentUI 전면 교체 — Task 1
- [x] EatDurationUI (먹기 진행) — Task 5, 9
- [x] 모듈화된 IAgentUI 구현체 — Task 3, 4, 5

**타입 일관성:**
- `IAgentUI`: `IsOpen`, `OpenAsync()`, `CloseAsync()` — Task 1에서 정의, Task 3/4/5에서 구현
- `IAgentUIModule`: `PlaySequenceAsync(CancellationToken, params IAgentUI[])` — Task 1 정의, Task 2 구현
- `DialogueSituation.WaitingOrder` — Task 6에서 enum 정의, Task 8에서 사용
- `_customer.CharacterData` — Task 6에서 `CustomerAgent`에 프로퍼티 추가, Task 8에서 사용
- `_workAction.OnProgressUpdated` — Task 7에서 추가, Task 9에서 구독
