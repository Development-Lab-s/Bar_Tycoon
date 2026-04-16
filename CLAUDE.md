# CLAUDE.md

## Project
- Unity 6 / URP 17
- 2D mobile game: Bar Tycoon
- 작업 범위: `Assets/00. Work/CheolYee/` 우선
- 다른 팀원 폴더는 수정하지 말 것
- 비동기: `UniTask`
- 트윈: LitMotion
- 입력: Input System

## Fixed Rules
- 구조 갈아엎기 금지
- 기존 클래스명/책임/흐름 유지
- 작은 단계로만 수정
- 한 턴 1~3파일 선호
- 최소 수정 우선
- 외부 연결은 이벤트/서비스, 내부 진행은 직접 호출 유지

## Story Architecture
외부  
→ `StoryCommandChannel`  
→ `StoryService`  
→ `StoryCoreFacade`  
→ `StoryRunner`  
→ `Directors / Controllers / Executors`

### Responsibility
- `StoryService`: 외부 진입, core 생성/재사용
- `StoryCoreFacade`: 세션, 열기/닫기, 스킵, UI 상태
- `StoryRunner`: 진행 루프만 담당
- Directors/Controllers: 분야별 연출
- `StoryExecutorRegistry`: 모듈 실행 디스패치

## Data Rules
- 데이터 = SO
- 실행 상태 = `StorySession`
- 라인 기본 구조는 공통 유지
- 연출은 모듈 추가 방식
- SO 무한 분화 금지
- 장기적으로 UI Toolkit 에디터 확장 가능해야 함

## Main Story Types
- Data: `CharacterDefinitionSO`, `StoryEpisodeSO`, `StoryLineSO`, `StoryModuleSO`, `StoryChoiceModuleSO`
- Runtime: `StorySession`, `StoryLogEntry`, `ActorRuntimeHandle`
- Execution: `IStoryModuleExecutor`, `StoryExecutorRegistry`
- Presentation: `StoryInputRouter`, `BasicStoryTextDirector`, `BasicStoryChoicePanelController`, `BasicStoryCharacterStageDirector`, `BasicStoryLogController`, `StorySkipSummaryPanelController`, `UIMotionPlayer`

## Current Progress
완료/거의 완료:
- 대사 출력, 이름 표시, 타이프라이터
- 탭 즉시 전체 출력 / 다음 진행
- 선택지 표시 / 선택 / 분기
- `Wait`, `CharacterClear`
- speaker 프리팹 자동 생성
- speaker 포커스 / 나머지 dim
- 기본 로그 적재
- 로그 패널 기본 열기/닫기
- 자동 진행 토글 보강
- 터치 블로커 / 레이캐스트 정리
- 스킵 UX 방향 확정
  - `SkipButton -> SkipSummaryPanel -> 닫기 / 스킵하기`
- 공용 LitMotion UI 모션 레이어 이미 있음

## Current Priorities
1. 스킵 UX 마감
   - `SkipSummaryPanel` 최종 동작
   - 입력 차단
   - 스킵 정책 정리
2. 로그 UX 마감
   - 모달 입력 차단
   - 자동 스크롤
   - 선택지 로그 스타일
3. UI 숨기기
   - `TopBar / Dialogue / Choice / Log` 숨김 및 복귀 규칙

## Next
- `TextSpeedModule`
- `TextShakeModule`
- `TextEmphasisModule`
- `CharacterMoveModule`
- `CharacterExpressionModule`
- `Background / SFX / BGM`
- 저장 / 이어보기
- UI Toolkit 커스텀 에디터

## Story Editor Tool (진행 중)

### 목표
Episode 선택 → Line 생성 → 시각적 연결 → 모듈 추가/수정을 하나의 EditorWindow에서 처리

### 제약
- 런타임 타입(`StoryLineSO`, `StoryEpisodeSO`, `StoryModuleSO` 등) 변경 금지
- `lineId` / `nextLineId` 문자열 구조 유지
- UI Toolkit 기반 EditorWindow 우선
- 한 번에 1~3파일, 최소 수정

### 모듈 SO 생성 정책
- 모듈은 에디터에서 자동 생성하여 Line에 부착
- 가능하면 Line asset의 **sub-asset**으로 생성 (폴더 clutter 방지)
- 런타임 타입(`StoryModuleSO` 서브클래스)은 그대로 유지

### 단계별 계획
1. **StoryLineSOEditor 개선** ← 완료
   - `lineId` 자동 생성 (에피소드 순서 기반 `line_001`)
   - suffix 지원, Undo, 중복/자기참조 경고
   - `nextLineId` 드롭다운 선택
2. **StoryEpisodeSOEditor** ← 다음
   - 빈 lineId 일괄 생성
   - 참조 무결성 검사 (끊긴 nextLineId 목록)
3. **Story EditorWindow (UI Toolkit)**
   - Episode 선택 패널
   - Line 목록 + 생성/삭제
   - Line 간 연결 시각화 (nextLineId 기반)
   - 모듈 추가 버튼 → sub-asset으로 자동 생성
   - 모듈 필드 인라인 편집

### 에디터 파일 위치
`Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Editor/`

## Design Patterns

### Inspector Interface Pattern
- Inspector 필드는 `MonoBehaviour` 타입으로 선언
- `Awake()`에서 `as IInterface`로 캐스트, `Debug.Assert`로 검증
- 인터페이스는 런타임 참조에만 사용, 직렬화하지 않음

```csharp
[SerializeField] private MonoBehaviour textDirectorBehaviour;
private IStoryTextDirector _textDirector;

private void Awake()
{
    _textDirector = textDirectorBehaviour as IStoryTextDirector;
    Debug.Assert(_textDirector != null, "textDirectorBehaviour must implement IStoryTextDirector");
}
```

### CanvasGroup 인터랙션 패턴
- 패널 열기: `motionPlayer.Play("Show", onComplete: () => SetCanvasGroupInteractable(true))`
- 패널 닫기: `motionPlayer.Play("Hide", onFinish: () => { IsOpen = false; ApplyClosedStateImmediate(); })`
- `onComplete`: 정상 완료 시만 실행 → 열기 완료 후 인터랙션 복구에 사용
- `onFinish`: 완료/취소 모두 실행(finally) → 닫기 완료 후 비활성화에 사용
- 애니메이션 중에는 반드시 `blocksRaycasts = false`로 차단

### UniTask 비동기 패턴
- 컴포넌트 생명주기: `this.GetCancellationTokenOnDestroy()` 또는 링크드 CTS
- fire-and-forget: `.Forget()` (단, `UniTaskVoid` 반환 메서드 권장)
- 취소 전파: `CancellationToken`을 항상 하위 호출까지 전달
- 복합 CTS: `CancellationTokenSource.CreateLinkedTokenSource(externalCt)` 사용

### ScrollRect RaycastTarget 규칙
- ViewPort 자식 아이템의 모든 Graphics가 `RaycastTarget=false`인 경우, ScrollRect가 드래그 이벤트를 수신하지 못함
- 해결: ViewPort에 `alpha=0, RaycastTarget=true` Image 컴포넌트 추가 (RectMask2D와 공존 가능)
- 아이템 prefab의 RaycastTarget 수정은 사이드이펙트가 있으므로 ViewPort에 적용하는 방식 사용

### 모듈 실행자 확장 패턴
- 새 연출 추가: `StoryModuleSO` 서브클래스 + `IStoryModuleExecutor` 구현체
- `CanExecute(module)` → 타입 체크, `ExecuteAsync(ctx, ct)` → 실제 연출
- `StoryExecutorRegistry`에 등록만 하면 `StoryRunner`가 자동 디스패치
- `StoryChoiceModuleSO`는 Runner가 직접 처리 (Registry 우회)

### 입력 상태 우선순위 (StoryInputRouter)
탭/클릭 시 아래 순서대로 체크:
1. Skip 팝업 열림 → 차단
2. Log 패널 열림 → 차단
3. UI 숨김 상태 → UI 복귀
4. 선택지 표시 중 → 선택지 확정
5. 선택지 패널 열림 → 차단
6. 타이핑 중 → 즉시 전체 출력
7. 대기 중 → 다음 라인 진행

### StorySession 상태 관리
- 런타임 상태의 단일 출처: `StorySession`
- `CurrentLineId`, `AdvanceMode`, `IsUiHidden`, `HasPendingResumePoint`
- `Logs`, `ChoiceResults`, `ActiveActors` 모두 Session에서 관리
- 세션 상태를 직접 읽지 말고 Facade가 제공하는 프로퍼티를 통해 접근

## Event Bus
`EventChannelSO`
- `AddListener<T>()`
- `RemoveListener<T>()`
- `RaiseEvent(new TEvent(...))`

스토리 채널:
- `storyCommandChannel`: Play / Resume / Close / Skip
- `storySignalChannel`: Opened / Started / Finished / Closed / ChoiceCommitted

## UIMotionPlayer
- Inspector 기반 `UIMotionSequence`
- `Play()`, `PlayAsync()`, `ApplyState()`
- 취소 시 최종 상태 적용

## Response Rule
항상:
1. 현재 상태 요약
2. 다음 한 단계
3. 수정/추가 파일
4. 코드
5. 인스펙터 연결
6. 완료 기준
7. 다음 단계 한 줄

## Avoid
- 전면 재설계
- “처음부터 다시”
- 과도한 동시 수정
- 현재 단계 무시한 완성형 제안
- 내부 진행을 전부 이벤트 채널화

## Style
- 한국어
- 존댓말
- 바로 적용 가능한 답변 우선
- 범위 밖 작업은 다음 단계로 미룸