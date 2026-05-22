# OrderBoardUI 수정 설계 정리

## 목표

Staff가 작업을 점유(InProgress)했을 때만 OrderTicketUI를 표시한다.
음료 완성 여부를 색으로, 진행 상태를 wave 애니메이션으로 표현한다.

---

## 핵심 결정사항

### 1. 표시 조건

| 상태 | 표시 |
|------|------|
| `OrderState.Waiting` | `SetActive(false)` |
| `OrderState.Reserved` | `SetActive(false)` |
| `OrderState.InProgress` | `SetActive(true)` |
| `OrderState.Done` | (Unregister → Destroy) |

→ `SetActive(false)` 선택 이유: 오브젝트를 미리 생성해두고 숨기는 방식이 텍스트 초기화보다 성능적으로 유리

### 2. 완성/미완성 기준 (WorkPhase 기준)

| WorkPhase | 상태 | `_foodName` 색 |
|-----------|------|--------------|
| `PendingCook` | 미완성 | `_incompleteColor` (인스펙터) |
| `ReadyForServe` 이상 | 완성 | `_completeColor` (인스펙터) |

→ 색상 2개를 `[SerializeField]`로 노출해 인스펙터에서 조정

### 3. Wave 애니메이션

- **대상:** `_workPhase` TMP 텍스트
- **방식:** LitMotion per-character wave (위아래 반복)
- **파라미터:** `_waveAmplitude`, `_waveFrequency` → `[SerializeField]` 인스펙터 노출
- **생명주기:** `SetActive(true)` 시 wave 시작, `SetActive(false)` 시 `MotionHandle` Cancel

---

## 이벤트 흐름

```
OrderRegisteredEvent
  └─ UI 생성 (Instantiate)
  └─ SetActive(false)          ← 아직 아무도 작업 안 함

OrderStateChangedEvent
  ├─ ticket.State == InProgress
  │   └─ SetActive(true)
  │   └─ _foodName 색 업데이트 (WorkPhase 기준)
  │   └─ _workPhase wave 시작
  └─ ticket.State != InProgress
      └─ SetActive(false)       ← 인터럽트 / 릴리즈 / 페이즈 전환
      └─ wave Cancel

OrderUnregisteredEvent
  └─ wave Cancel
  └─ Destroy(gameObject)
```

---

## 수정 대상 파일

| 파일 | 변경 내용 |
|------|----------|
| `OrderTicketUI.cs` | SetActive 로직, 색상 필드 추가, LitMotion wave 추가 |
| `OrderBoardUI.cs` | `OnRegistered`에서 생성 후 숨김 처리 |

---

## 건드리지 않는 것

- `_stateBadge` — 기존 동작 유지
- `_priceLabel` — 기존 동작 유지
- `OrderManager`, `OrderTicket`, 이벤트 클래스 — 변경 없음
