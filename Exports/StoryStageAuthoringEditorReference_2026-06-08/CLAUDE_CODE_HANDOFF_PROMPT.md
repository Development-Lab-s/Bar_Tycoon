# Claude Code Handoff Prompt

아래 프롬프트를 Claude Code 에 그대로 보내면 된다.

```text
지금부터 너의 작업은 "이 reference 폴더를 정확히 읽고, 내가 다른 프로젝트에서 비슷한 구조의 키프레임 에디터를 구현할 수 있도록 구조를 해설하는 것"이다.

중요:
- 대충 요약하지 마라.
- 반드시 파일 경로, 클래스명, 메서드명을 인용해서 설명해라.
- UI 상호작용이 어떤 함수 호출로 이어지는지 단계별로 적어라.
- "무슨 데이터를 어디에 저장하는지"와 "무슨 함수가 단순 refresh 인지"를 구분해서 적어라.
- 특히 아래 4개 선택 타입을 각각 분리해서 설명해라:
  - Actor
  - Background
  - Camera
  - Sound

내 목적:
- 나는 다른 프로젝트에서 기능이 다른 에디터를 만들고 있다.
- 구조는 거의 비슷하다.
- 하지만 대상은 스토리 2D 무대 연출이 아니라, 스킬 연출 에디터다.
- 3D 렌더링과 3D 프리뷰 기능을 붙일 예정이지만,
- 오브젝트 선택 -> 타임라인 row 표시 -> 키프레임 생성/수정 -> 인스펙터 수정 -> 프리뷰 반영
  이 전체 편집 구조는 여기 reference 를 최대한 그대로 가져가고 싶다.

먼저 이 폴더를 읽어라.

읽기 시작 파일:
1. `CLAUDE_CODE_STAGE_AUTHORING_WALKTHROUGH.md`
2. `README.md`
3. `FILE_MAP.md`

그 다음 가능하면 아래 스크립트를 실행해서 핵심 메서드 위치를 확인해라:
- `powershell -ExecutionPolicy Bypass -File .\scripts\Show-StageAuthoringFlow.ps1 -Section All`
- 필요하면 다음도 실행해라:
  - `powershell -ExecutionPolicy Bypass -File .\scripts\Show-StageAuthoringFlow.ps1 -Section Selection`
  - `powershell -ExecutionPolicy Bypass -File .\scripts\Show-StageAuthoringFlow.ps1 -Section Timeline`
  - `powershell -ExecutionPolicy Bypass -File .\scripts\Show-StageAuthoringFlow.ps1 -Section Inspector`
  - `powershell -ExecutionPolicy Bypass -File .\scripts\Show-StageAuthoringFlow.ps1 -Section Viewport`
  - `powershell -ExecutionPolicy Bypass -File .\scripts\Show-StageAuthoringFlow.ps1 -Section Persistence`

그 다음 아래 순서로 소스를 읽어라:
1. `source/Editor/StoryPreviewWindow.cs`
2. `source/Editor/StoryPreviewWindow.Layout.cs`
3. `source/Editor/StoryPreviewWindow.Inspector.cs`
4. `source/Editor/StoryPreviewWindow.Timeline.cs`
5. `source/Editor/StoryPreviewWindow.Sound.cs`
6. `source/Editor/StoryPreviewWindow.Actors.cs`
7. `source/Editor/StoryPreviewWindow.Background.cs`
8. `source/Editor/StoryPreviewWindow.Playback.cs`
9. `source/Data/Modules/StoryStageLayoutModuleSO.cs`
10. `source/Data/Modules/StoryActorKeyframeData.cs`
11. `source/Shared/StoryTransitionSampler.cs`

설명할 때 반드시 아래 질문에 답해라.

1. 현재 라인을 선택하면 어떤 경로로 preview 창까지 전달되는가?
2. 오른쪽 오브젝트 목록(하이러키 역할)은 실제로 어디서 만들어지는가?
3. Actor / Background / Camera / Sound 를 선택하면 각각 어떤 `Select*` 함수로 들어가고, 어떤 상태값이 바뀌는가?
4. selection 이 바뀐 뒤 타임라인은 어디서 다시 그려지는가?
5. selection 타입별로 어떤 property row 가 표시되는가?
6. property row 는 "빈 row 생성"인지, 아니면 "첫 key 가 생긴 결과 row 가 보이는 것"인지?
7. `Add Property`, `Add Key`, row 우클릭, key 클릭, key drag, segment 선택이 각각 어떤 함수로 이어지는가?
8. 인스펙터에서 선택된 key 값을 수정할 때 실제로 어떤 저장 함수로 내려가는가?
9. viewport 에서 Actor / Background / Camera 를 직접 드래그할 때,
   - snapshot 수정인지
   - selected key 수정인지
   - record 모드에서 새 key 생성인지
   이 분기가 어디서 일어나는가?
10. Sound 는 왜 일반 keyframe 과 다르게 proxy 모델을 쓰는가?
11. 다른 프로젝트에서 3D 스킬 연출 에디터로 옮길 때
   - 그대로 유지할 구조
   - 3D 용으로 바꿔야 할 부분
   - 먼저 복제해야 하는 최소 골격
   을 어떻게 나눌 수 있는가?

응답 형식:

## 1. 한 줄 요약
- 이 에디터의 구조를 한 문장으로 요약

## 2. 시스템 지도
- 주요 파일 역할
- source of truth 구조
- snapshot / track / sampled preview 차이

## 3. 사용자 액션별 흐름
- "라인 선택"
- "오브젝트 선택"
- "타임라인 row 생성"
- "키프레임 선택"
- "키프레임 시간 이동"
- "인스펙터 값 수정"
- "viewport 직접 조작"
각 항목마다 "어떤 UI 입력 -> 어떤 함수 -> 어떤 저장 -> 어떤 refresh" 인지 단계별로 적어라.

## 4. 선택 타입별 상세 분석
- Actor
- Background
- Camera
- Sound
각 타입마다 아래를 적어라:
- selection entry point
- 타임라인 row 종류
- key 데이터 타입
- 인스펙터 편집 필드
- 저장 함수
- preview 반영 함수

## 5. 포팅용 추상화 제안
- 내가 3D 스킬 연출 에디터로 바꿀 때 유지해야 할 인터페이스/책임 분리
- `SelectionContext`, `TrackStore`, `PreviewSampler`, `ViewportManipulator`, `InspectorBuilder` 같은 식으로 추상화 후보가 있으면 제안
- 단, 불필요한 추상화는 하지 마라. 현재 구조에 맞춰 최소한으로 제안해라.

## 6. 구현 착수용 체크리스트
- 다른 프로젝트에서 가장 먼저 복제할 순서
- 그 다음 3D 전용으로 갈아끼울 순서

중요 제약:
- vague summary 금지
- 반드시 함수명과 파일 경로를 적어라
- 가능하면 함수 호출 순서를 화살표로 적어라
- "이 함수는 데이터를 저장한다 / 이 함수는 UI refresh만 한다 / 이 함수는 sampled preview를 만든다"를 명시해라
- 특히 Sound proxy 모델, Record 모드, selection-kind 기반 UI 분기를 빠뜨리지 마라

마지막에는 내가 실제 구현에 바로 착수할 수 있게,
"이 구조를 스킬 연출 에디터로 복제할 때의 최소 구현 순서"를 10~20개 체크리스트로 정리해라.
```

