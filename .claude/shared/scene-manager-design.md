# SceneManager 설계 문서

## 1. 씬 구조

| 씬 | 역할 | 비고 |
|---|---|---|
| Bootstrap | DontDestroyOnLoad 오브젝트 생성 후 Title 로드, 자기 자신 언로드 | 씬 0번 |
| Title | 타이틀 화면 | |
| Main | 타이쿤 루프 베이스 씬 | 항상 로드 유지 |
| Story | 스토리 연출 | Main 위에 Additive |
| Cocktail | 칵테일 미니게임 | Main 위에 Additive |

---

## 2. 씬 전환 흐름

```
Bootstrap → Title         (Replace, 초기화 후 즉시)
Title     → Main          (Replace, FadeOut → Load → SceneReady → FadeIn)
Main     ↔  Story         (Additive, FadeOut → Load/Unload → SceneReady → FadeIn)
Main     ↔  Cocktail      (Additive, FadeOut → Load/Unload → SceneReady → FadeIn)
```

- Story ↔ Cocktail 직접 전환 없음
- Main은 게임 진행 중 언로드되지 않음

---

## 3. SceneManager

- **위치**: Bootstrap 씬에서 생성, `DontDestroyOnLoad`
- **같은 오브젝트에 포함**: FadeUI Canvas, FirstLoader
- **내부 API**: `TransitionTo(SceneType target)`
- **외부 접근**: SceneChannel의 `SceneTransitionRequestEvent` 구독으로만 호출
- **상태 관리**: `SceneType` enum으로 현재 활성 씬 추적

```csharp
public enum SceneType { Title, Main, Story, Cocktail }
```

---

## 4. FadeIn/Out

- **소유**: DontDestroyOnLoad Canvas (SceneManager 오브젝트)
- **페이드아웃**: 씬 전환 요청 즉시 시작
- **페이드인 트리거**: `SceneReadyEvent` 수신 시 시작
- **Duration**: 하드코딩 (SO 없음)
- **SceneReady 발행 시점**: 각 씬의 초기화 완료 후 (단순 씬은 `Start()`에서 즉시 발행)

---

## 5. SceneChannel (EventChannelSO)

SceneManager 전용 EventChannelSO. 이벤트 3개:

| 이벤트 | 방향 | 설명 |
|---|---|---|
| `SceneTransitionRequestEvent(SceneType)` | 외부 → SceneManager | 씬 전환 요청 |
| `SceneReadyEvent` | 씬 → SceneManager | 씬 초기화 완료, 페이드인 허가 |
| `SceneTypeChangedEvent(SceneType)` | SceneManager → 전체 | 현재 활성 씬 브로드캐스트 |

---

## 6. 백그라운드 모드 (Story / Cocktail 활성 중)

Main 씬은 Additive로 살아있지만 아래 항목들을 억제한다.

### 비활성화
| 대상 | 처리 |
|---|---|
| Main Camera | `SetActive(false)` |
| Main Canvas | `SetActive(false)` |

→ `MainSceneVisibility` 컴포넌트 하나로 Camera + Canvas 묶어서 관리

### 억제 항목

| 시스템 | 동작 | 방식 |
|---|---|---|
| CustomerManager | 새 손님 스폰 중단 | `SceneTypeChangedEvent` 구독, 로컬 bool 플래그 |
| CustomerAgent | 대기 타이머 일시정지 | `SceneTypeChangedEvent` 구독 |
| ScheduleManager | `ScheduleRequestEvent` 구독 해제 | `SceneTypeChangedEvent` 구독 → 재구독 시 pending 큐 자동 소화 |
| SoundManager | 현재 사운드 페이드아웃, 새 사운드 차단 | `SceneTypeChangedEvent` 구독 해제 방식 |

### 계속 진행
- 직원 진행 중인 작업 (`WorkExecutionContext`)
- 주문 상태 처리

---

## 7. Bootstrap 씬 초기화 순서

1. FirstLoader (`ExecutionOrder -20`) 실행
   - `Time.timeScale = 1.0f`
   - UIChannel, PlayerChannel, SystemChannel 초기화
2. SceneManager + FadeUI 생성 → `DontDestroyOnLoad`
3. `TransitionTo(SceneType.Title)` 호출
4. Bootstrap 씬 언로드

---

## 8. 각 씬의 SceneReady 발행 책임

| 씬 | SceneReady 발행 시점 |
|---|---|
| Title | `Start()` 즉시 |
| Main | `GameLoader` 복원 완료 후 |
| Story | 스토리 시스템 초기화 완료 후 |
| Cocktail | 칵테일 UI 초기화 완료 후 |
