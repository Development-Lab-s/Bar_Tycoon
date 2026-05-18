# AgentStatusUI Design

**Date:** 2026-05-17
**Branch:** BBJ
**Scope:** CustomerWaitUI + WorkIconUI 병합, Customer/Staff 공용 상태 표시 UI

---

## 배경

기존에 `CustomerWaitUI`와 `WorkIconUI`가 별도로 존재했지만 둘 다 실제 구현 없이 `Open()`/`Close()`만 있는 껍데기였다. 이를 하나의 `AgentStatusUI`로 통합하고, 아이콘과 텍스트를 실제로 표시하도록 구현한다.

---

## 스코프

- **포함:** `AgentStatusUI` 신규 클래스, 기존 두 클래스 삭제, State 수정
- **제외:** `WorkDurationUI` 리팩터링 (나중에 별도 진행)

---

## 컴포넌트 설계

### `AgentStatusUI : MonoBehaviour, IAgentUI`

```
[SerializeField] Image    _icon
[SerializeField] TMP_Text _label

Awake()                   → gameObject.SetActive(false)
Open()                    → gameObject.SetActive(true)
Close()                   → gameObject.SetActive(false)
SetIcon(Sprite sprite)    → _icon.sprite = sprite; _icon.gameObject 활성/비활성
SetText(string text)      → _label.text = text; _label.gameObject 활성/비활성
```

- `SetIcon(null)` → `_icon` 오브젝트 비활성
- `SetText("")` 또는 `SetText(null)` → `_label` 오브젝트 비활성
- 아이콘만, 텍스트만, 둘 다 동시 표시 모두 가능
- `namespace BBJ.UI`

---

## IAgentUI 인터페이스

변경 없음. `Refresh(Agent)` 추가 불필요 — State가 데이터 접근 후 직접 `SetIcon()`/`SetText()` 호출하므로.

---

## State 수정

### `CustomerIdleState`

```csharp
// 기존: CustomerWaitUI 참조
// 변경: AgentStatusUI 참조

Enter():
  _statusUI.SetIcon(/* customer.SelectedFood?.icon 등 */);
  _uiModule.SetActiveUI<AgentStatusUI>(true);

Exit():
  _uiModule.SetActiveUI<AgentStatusUI>(false);
```

- `owner as CustomerAgent` 캐스팅으로 `SelectedFood`, `ActiveTicket` 등 접근
- 표시할 정보는 구현 시 CustomerAgent 상태에 맞게 결정

### `StaffWorkState`

```csharp
// 기존: WorkIconUI 참조
// 변경: AgentStatusUI 참조

Enter():
  _statusUI.SetIcon(/* 작업 아이콘 */);
  _uiModule.SetActiveUI<AgentStatusUI>(true);

Exit():
  _uiModule.SetActiveUI<AgentStatusUI>(false);
```

- 작업 아이콘은 `IAgentActionModule` 또는 현재 WorkSO에서 접근

---

## 삭제 대상

- `Assets/00. Work/BBJ/02. Scripts/UI/CustomerWaitUI.cs`
- `Assets/00. Work/BBJ/02. Scripts/UI/WorkIconUI.cs`

---

## 프리팹 / 인스펙터 작업

- 기존 Customer/Staff Agent 프리팹에서 `CustomerWaitUI`, `WorkIconUI` 컴포넌트를 `AgentStatusUI`로 교체
- `_icon` (Image), `_label` (TMP_Text) 레퍼런스 연결
- `AgentUIModule`은 `GetComponentsInChildren<IAgentUI>()` 자동 수집이므로 추가 등록 불필요

---

## 검증 방법

1. Customer가 대기 상태로 진입 시 `AgentStatusUI`가 열리고 아이콘/텍스트가 표시됨
2. Customer가 이동/작업 상태로 전환 시 `AgentStatusUI`가 닫힘
3. Staff가 작업 상태 진입 시 `AgentStatusUI`가 열리고 작업 아이콘 표시됨
4. Staff가 작업 종료 시 `AgentStatusUI`가 닫힘
5. 컴파일 에러 없음, 콘솔 에러 없음
