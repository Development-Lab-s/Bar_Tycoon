# Agent UI Sequence System — Design Spec
Date: 2026-05-24

## Overview

Customer 에이전트가 주문 대기 진입 시, 대사 말풍선을 먼저 표시한 뒤 메뉴 상태 UI로 전환하는 시퀀스 시스템을 구현한다. LitMotion 기반 애니메이션, IAgentUI 전면 재설계, AgentUIModule 시퀀스 지원을 포함한다.

---

## 1. 대사 데이터 — CharacterDataSO

대사 라인은 `CharacterDataSO`에 상황(Situation)별 풀로 정의된다.

```csharp
public enum DialogueSituation
{
    WaitingOrder,   // 주문 대기 진입 시 ("달달한 걸로 주세요" 등)
    // 향후 확장 가능
}

[Serializable]
public struct DialogueLine
{
    public DialogueSituation Situation;
    public string[]          Lines;  // 랜덤 선택 풀
}

// CharacterDataSO에 추가
[SerializeField] public DialogueLine[] DialogueLines;

public string GetLine(DialogueSituation situation)
{
    var match = System.Array.Find(DialogueLines, d => d.Situation == situation);
    return match.Lines?.Length > 0
        ? match.Lines[UnityEngine.Random.Range(0, match.Lines.Length)]
        : string.Empty;
}
```

- 라인이 없으면 빈 문자열 반환 → 대사 없이 메뉴 UI만 표시하는 경우도 지원

---

## 2. IAgentUI 인터페이스 — 전면 교체

```csharp
public interface IAgentUI
{
    bool IsOpen { get; }
    UniTask OpenAsync();
    UniTask CloseAsync();
}
```

- `OpenAsync()`: 열기 애니메이션 재생, 완료 시 resolve
- `CloseAsync()`: 닫기 애니메이션 재생, 완료 시 resolve
- `IsOpen`: 현재 열림 여부 (시퀀스 중 중복 호출 방어)

---

## 3. IAgentUIModule 인터페이스 — 업데이트

```csharp
public interface IAgentUIModule
{
    T Get<T>() where T : class, IAgentUI;
    UniTask PlaySequenceAsync(CancellationToken ct, params IAgentUI[] sequence);
    void CancelSequence();
    void CloseAll();
}
```

- `PlaySequenceAsync`: 각 UI를 순서대로 `OpenAsync → CloseAsync` 실행. 마지막 UI는 `CloseAsync` **미호출** (열린 채로 유지, 외부에서 제어)
- `CancelSequence`: 진행 중 시퀀스 즉시 취소
- `CloseAll`: 시퀀스 취소 + 모든 UI `CloseAsync` 호출

---

## 4. AgentUIModule 구현

```csharp
public class AgentUIModule : MonoBehaviour, IModule, IAgentUIModule
{
    private Dictionary<Type, IAgentUI> _uis;
    private CancellationTokenSource    _sequenceCts;

    public void Initialize(ModuleOwner owner)
    {
        _uis = GetComponentsInChildren<IAgentUI>(true)
            .ToDictionary(ui => ui.GetType());
    }

    public T Get<T>() where T : class, IAgentUI { ... }

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
```

---

## 5. 신규 UI 클래스

### DialogueBubbleUI

```csharp
public class DialogueBubbleUI : MonoBehaviour, IAgentUI
{
    [SerializeField] private TMP_Text    _label;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float       _displayDuration = 1.5f;
    [SerializeField] private float       _animDuration    = 0.2f;

    public bool IsOpen { get; private set; }

    public void SetText(string text) => _label.text = text;

    public async UniTask OpenAsync()
    {
        gameObject.SetActive(true);
        IsOpen = true;
        // LitMotion: alpha 0→1
        await LMotion.Create(0f, 1f, _animDuration)
            .BindToCanvasGroupAlpha(_canvasGroup)
            .AddTo(this);
        await UniTask.Delay(TimeSpan.FromSeconds(_displayDuration));
    }

    public async UniTask CloseAsync()
    {
        // LitMotion: alpha 1→0
        await LMotion.Create(1f, 0f, _animDuration)
            .BindToCanvasGroupAlpha(_canvasGroup)
            .AddTo(this);
        gameObject.SetActive(false);
        IsOpen = false;
    }
}
```

- `OpenAsync`는 fade-in + `_displayDuration` 대기까지 포함
- `CloseAsync`는 fade-out만 담당
- 시퀀스에서: `OpenAsync` (대기 포함) 완료 → `CloseAsync` 호출

### AgentStatusUI (수정)

- 기존 `SetIcon`, `SetText`, `SetIconColor` 유지
- `OnOpen`/`OnClose` → `OpenAsync`/`CloseAsync`로 교체
- `OpenAsync`: scale 0→1 bounce (LitMotion)
- `CloseAsync`: alpha 1→0 fade (LitMotion)

### EatDurationUI (신규)

```csharp
public class EatDurationUI : MonoBehaviour, IAgentUI, IModule
{
    [SerializeField] private Slider _slider;
    [SerializeField] private float  _animDuration = 0.15f;

    public bool IsOpen { get; private set; }

    public void Initialize(ModuleOwner owner) { Disable(); }

    public void SetPercent(float value) => _slider.value = Mathf.Clamp01(value);

    public async UniTask OpenAsync()
    {
        gameObject.SetActive(true);
        IsOpen = true;
        await LMotion.Create(0f, 1f, _animDuration)
            .Bind(v => _slider.transform.localScale = Vector3.one * v)
            .AddTo(this);
    }

    public async UniTask CloseAsync()
    {
        await LMotion.Create(1f, 0f, _animDuration)
            .Bind(v => _slider.transform.localScale = Vector3.one * v)
            .AddTo(this);
        gameObject.SetActive(false);
        IsOpen = false;
    }

    private void Disable() { gameObject.SetActive(false); IsOpen = false; }
}
```

- `WorkAction.OnWorkPhaseStarted` → `OpenAsync()` (eating 단계)
- `WorkAction.OnWorkPhaseEnded` → `CloseAsync()`

---

## 6. CustomerIdleState 호출 흐름

```csharp
// CustomerIdleState.Enter() 내 — 주문 대기 진입 시
private CancellationTokenSource _uiCts;

public override void Enter()
{
    base.Enter();
    _uiCts = new CancellationTokenSource();
    // ...
    PlayOrderSequenceAsync().Forget();
}

public override void Exit()
{
    base.Exit();
    _uiCts?.Cancel();
    _uiCts?.Dispose();
    _uiCts = null;
    _uiModule.CloseAll();
}

private async UniTaskVoid PlayOrderSequenceAsync()
{
    var dialogue = _uiModule.Get<DialogueBubbleUI>();
    var status   = _uiModule.Get<AgentStatusUI>();

    var line = _character.CharacterData?.GetLine(DialogueSituation.WaitingOrder);
    if (!string.IsNullOrEmpty(line))
    {
        dialogue.SetText(line);
        await _uiModule.PlaySequenceAsync(_uiCts.Token, dialogue, status);
    }
    else
    {
        await _uiModule.PlaySequenceAsync(_uiCts.Token, status);
    }

    RefreshUI(); // 마지막 statusUI 내용 갱신
}
```

---

## 7. 별도 고려사항

| 항목 | 결정 |
|---|---|
| `PlaySequenceAsync` 마지막 UI | `CloseAsync` 미호출, 열린 채로 유지 |
| 시퀀스 취소 시점 | `CustomerIdleState.Exit()`에서 `_uiCts.Cancel()` + `CloseAll()` |
| `DialogueBubbleUI.OpenAsync` | fade-in + displayDuration 대기 포함 (CloseAsync 없이도 자연스러운 흐름) |
| `EatDurationUI` 트리거 | `WorkAction.OnWorkPhaseStarted/Ended` — CustomerWorkState에서 호출 |
| `CharacterDataSO.GetLine` | 라인 없으면 빈 문자열 → 대사 건너뜀 |
| `WorkDurationUI` | 기존 Staff용 유지, Customer는 별도 `EatDurationUI` 사용 |

---

## 변경 파일 목록

| 파일 | 변경 종류 |
|---|---|
| `IAgentUI.cs` | 전면 교체 |
| `IAgentUIModule.cs` | 메서드 추가 |
| `AgentUIModule.cs` | PlaySequenceAsync, CancelSequence 추가 |
| `AgentStatusUI.cs` | async 패턴으로 교체 |
| `DialogueBubbleUI.cs` | 신규 |
| `EatDurationUI.cs` | 신규 |
| `CharacterDataSO.cs` | DialogueLine 추가 |
| `CustomerIdleState.cs` | PlayOrderSequenceAsync 추가 |
| `CustomerWorkState.cs` | EatDurationUI 연결 |
