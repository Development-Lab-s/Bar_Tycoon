# Next Session Start Prompt

```text
unity-mcp 스킬과 grill-me 스킬만 로드해. Superpowers 계열 스킬은 로드하지도 말고 사용하지도 마.

그 다음 순서대로 진행해:

1. unity-mcp가 연결되었는지 확인하고, 연결된 Unity 인스턴스가 여러 개면 활성 인스턴스를 먼저 고정해.
2. `CLAUDE.md`를 읽고 작업 지침을 확인해.
3. `CLAUDE.local.md` 파일을 찾되, 이 프로젝트에는 실파일이 없을 수 있다. 없으면 아래 "현재 세션 요약"을 `CLAUDE.local.md` 대체 컨텍스트로 사용해.
4. 구현 전에 먼저 reference 폴더를 읽어:
   - `Exports/StoryStageAuthoringEditorReference_2026-06-08/CLAUDE_CODE_STAGE_AUTHORING_WALKTHROUGH.md`
   - `Exports/StoryStageAuthoringEditorReference_2026-06-08/README.md`
   - `Exports/StoryStageAuthoringEditorReference_2026-06-08/FILE_MAP.md`
5. 가능하면 아래 스크립트도 실행해서 reference 코드의 핵심 메서드 위치를 확인해:
   - `powershell -ExecutionPolicy Bypass -File .\Exports\StoryStageAuthoringEditorReference_2026-06-08\scripts\Show-StageAuthoringFlow.ps1 -Section All`
6. 그 다음 현재 프로젝트 안에서 실제 스킬 연출 에디터 관련 파일들을 찾아:
   - EditorWindow 본체/partial 파일
   - `SkillPresentationDataSO`
   - `SkillPresentationTimeline`
   - 현재 keyframe 데이터 타입
   - runtime sampler / player / executor
7. 아직 바로 구현하지 마. 먼저 grill-me로 아래 미확정 항목 2개를 확정해:
   - partial 파일 재배분
   - 기존 SO 에셋 마이그레이션 전략
8. 확정 후 Bug 4를 여러 페이즈로 쪼개고, 각 페이즈마다 수정 파일 / 검증 기준 / 리스크를 짧게 정리해.
9. 그 다음에만 구현을 시작해. 큰 범위로 한 번에 건드리지 말고, phase 단위로 작게 진행해.

중요한 작업 목표:
- 지금 해야 할 것은 `Bug 4 — 에디터 타임라인 구조 개편`이다.
- `Bug 5 — 이징 연결선 표시`는 Bug 4 완료 후 진행한다.
- Story reference 구조를 스킬 에디터에 "그대로 복붙"하는 게 아니라, 구조를 유지한 채 스킬 시스템에 맞게 축소 이식한다.

반드시 먼저 파악해야 할 핵심:
- 이 에디터는 selection-kind 기반 UI 구조로 가야 한다.
- 오브젝트 선택 -> 타임라인 row 표시 -> 첫 key 생성 시 row 등장 -> key 선택/수정 -> 인스펙터 반영 -> preview 반영
  이 흐름을 Story reference와 동일한 방식으로 재구성해야 한다.
- row는 빈 UI가 아니라, 해당 property keyframe 존재 여부의 시각화다.
- VFX는 Story의 actorTracks에 해당하는 독립 object selection 모델로 간다.
- `spawnTime`은 타임라인 row가 아니라 인스펙터 값이다.

이번 세션에서 이미 확정된 아키텍처는 아래와 같다.

---
현재 세션 요약
---

프로젝트 컨텍스트:
- Unity 6 기반 3D 턴제 카드 전투 게임
- 행동 큐 없음, 카드 드롭 즉시 발동
- 비동기: UniTask
- 트위닝: LitMotion
- DI: Reflex
- 동적 스폰 오브젝트는 `GameObjectInjector.InjectRecursive(go, _container)`

작업 규칙:
- Superpowers 계열 스킬 사용 금지
- 테스트 코드는 명시 요청 시만 추가
- 구현 전 반드시 grill-me로 구조 확정

현재 다음 작업 순서:
1. Skill Presentation System 완료
2. Bug 4 — 에디터 타임라인 구조 개편 ← 지금 해야 할 것
3. Bug 5 — 이징 연결선 표시

Bug 4 확정 아키텍처 방향:
- `StoryStageAuthoringEditorReference_2026-06-08` 구조를 스킬 에디터에 이식
- 참고 핵심 파일:
  - `StoryPreviewWindow.cs`
  - `StoryPreviewWindow.Inspector.cs`
  - `StoryPreviewWindow.Timeline.cs`

데이터 모델 방향:

```csharp
[Serializable]
class SkillPresentationTimeline {
    SkillSingleTrackData animationTrack;
    SkillSingleTrackData effectTrack;
    List<SkillVfxObjectData> vfxObjects;
    SkillSingleTrackData cameraTrack;
    SkillSingleTrackData uiTrack;
    SkillSingleTrackData sfxTrack;
}

[Serializable]
class SkillSingleTrackData {
    List<SkillKeyframeData> keyframes;
}

[Serializable]
class SkillVfxObjectData {
    VfxKey vfxKey;
    float spawnTime;
    float lifeTime;
    SpawnTarget spawnTarget;
    Vector3 spawnPositionOffset;
    Vector3 spawnRotationEuler;
    List<SkillKeyframeData> keyframes;
}

[Serializable]
class SkillKeyframeData {
    SkillKeyframeProperty property;
    float timeSeconds;

    AnimParamSO animParam;

    string effectSlotId;
    float valueMultiplier;

    Vector3 position;
    Vector3 rotationEuler;
    Vector3 scale;
    bool isHold;
    Ease easing;

    SkillCameraFocusTarget focusTarget;
    Vector3 cameraPosition;
    Vector3 cameraRotationEuler;
    float fieldOfView;
    float amplitude;
    float shakeDuration;

    SkillUiAction uiAction;

    string sfxId;
}
```

핵심 원칙:
- row는 `HasProperty(track, property)`가 true일 때만 보인다
- row는 빈 UI가 아니라 keyframe 존재 여부의 결과다
- 모든 key는 스킬 절대 시간(`timeSeconds`) 기준이다

선택 상태 관리:

```csharp
SkillObjectKind _selectedObjectKind;   // None, Animation, Effect, Vfx, Camera, Ui, Sfx
int _selectedVfxIndex;                 // Vfx 선택 시 인덱스, 없으면 -1
```

```csharp
enum SkillObjectKind { None, Animation, Effect, Vfx, Camera, Ui, Sfx }
```

확정된 UX 흐름:

```text
오브젝트 버튼 클릭
  -> SelectObject(kind, vfxIndex?)
  -> _selectedObjectKind = kind
  -> RefreshObjectList()
  -> RefreshTimeline()
  -> RefreshInspector()

타임라인이 비어 있을 때 Add Property 클릭
  -> ShowAddPropertyMenu()
  -> 프로퍼티 선택
  -> AddPropertyKey(property)
  -> playhead 위치에 첫 keyframe 생성
  -> RefreshTimeline()
  -> row 표시

키프레임 전부 삭제
  -> HasProperty() == false
  -> row 자동 제거
```

VFX 오브젝트 설계:
- Add Object -> VFX 선택 시 `SkillVfxObjectData` 생성
- 오브젝트 목록에 `VFX [0]`, `VFX [1]` 등으로 표시
- 선택 시 해당 VFX의 row만 타임라인에 표시
- `spawnTime`은 인스펙터에서 편집
- 타임라인 row는 `VfxPosition`, `VfxRotation`, `VfxScale`

인스펙터 패널 구조:

```text
InspectorPanel
├── ObjectListPanel
│   ├── Add Object 드롭다운
│   └── Animation / Effect / VFX[n] / Camera / UI / SFX 버튼
└── KeyframeInspectorPanel
    └── 선택된 keyframe 값 편집
```

다음 세션에서 반드시 먼저 확정해야 할 미확정 항목:
1. partial 파일 재배분
   - `ObjectList.cs` 신규 추가 여부
   - `Timeline.cs`의 `Build*TimelineRows()` 분배 방식
   - `Inspector.cs`에서 ObjectListPanel / KeyframeInspectorPanel 분리 범위
2. 기존 SO 에셋 마이그레이션 전략
   - 수동 ContextMenu
   - `OnValidate()` 자동 마이그레이션
   - 기존 에셋 포기 후 재작성

구현할 때 지켜야 할 것:
- 반드시 phase 단위로 작게 구현
- 먼저 selection model과 data model부터 정리
- 그 다음 object list / timeline row 표시
- 그 다음 Add Property / Add Key
- 그 다음 keyframe inspector
- 그 다음 preview 반영
- Bug 5는 Bug 4 끝나기 전 건드리지 마

시작 응답 형식:
1. 지금 읽은 파일과 확인한 Unity 연결 상태 요약
2. 현재 프로젝트에서 실제로 수정해야 할 파일 후보 목록
3. grill-me로 먼저 확정해야 할 질문 2~4개
4. Bug 4를 3~6개 phase로 쪼갠 구현 순서 제안

구현은 내가 grill-me 응답을 마친 뒤에 시작해.
```

