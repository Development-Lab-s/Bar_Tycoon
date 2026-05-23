# Story Sound Authoring Unification Design

**Date:** 2026-05-22
**Area:** Story Preview sound authoring, timeline UX, episode-level sound defaults
**Primary Files:**
- `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/StoryEpisodeSO.cs`
- `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Modules/StoryStageLayoutModuleSO.cs`
- `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Modules/StorySoundTrackData.cs`
- `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Core/StoryRunner.cs`
- `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Editor/StoryPreviewWindow.cs`
- `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Editor/StoryPreviewWindow.Inspector.cs`
- `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Editor/StoryPreviewWindow.Sound.cs`
- `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Editor/StoryPreviewWindow.Timeline.cs`

---

## Goal

스토리 프리뷰의 사운드 authoring을 기존 액터/배경/카메라 타임라인 규칙과 동일하게 맞춘다.

이번 변경의 목표는 다음 네 가지다.

- 사운드 타임라인을 기존 공용 타임라인 UX와 같은 규칙으로 통일
- 반복적인 line 단위 sound settings 입력을 episode 기본값 + line override 구조로 정리
- Delete, Undo, Redo, 다중 선택, 드래그 충돌 처리의 즉시성 확보
- 기존 런타임 사운드 동작은 유지하면서 editor authoring 흐름만 개선

---

## Problem Statement

현재 사운드 트랙은 런타임 기능은 대부분 동작하지만, editor authoring 규칙이 기존 타임라인과 어긋난다.

- `Sound` selection은 존재하지만, `BGM`/`SFX` row가 항상 자동 생성되어 `Add Property` 규칙과 맞지 않는다.
- row 우클릭 `Add Key`, key drag, box select, ctrl multi-select, group drag가 사운드에는 연결되어 있지 않다.
- Delete, Undo, Redo가 사운드에서는 기존 트랙만큼 즉시 반영되지 않는다.
- `Sound Channel`, `BGM Fade Duration`, `SFX Fade Duration`이 매 line마다 직접 할당되어 불편하다.
- 이미 기존 line asset들에 `soundTrack`과 `soundSettings`가 들어가 있으므로, 새 구조는 content-safe하게 호환되어야 한다.

---

## Chosen Approach

### 1. Sound Runtime Data는 유지한다

`StoryBgmKeyframeData`와 `StorySfxKeyframeData`는 유지한다.

사운드를 `StoryActorKeyframeData` 기반 공용 데이터 모델로 마이그레이션하지 않는다. 대신 editor 쪽 타임라인 상호작용 계층을 공용화해서 사운드도 같은 규칙을 쓰게 만든다.

이 선택의 이유:

- 런타임 직렬화 구조를 크게 흔들지 않는다.
- 기존 사운드 자산을 content-safe하게 유지할 수 있다.
- 필요한 변화는 대부분 editor interaction 규칙 정리에 있다.

### 2. Sound Settings는 Episode Default + Whole-Block Line Override로 간다

- `StoryEpisodeSO`가 episode 단위 `defaultSoundSettings`를 가진다.
- `StoryStageLayoutModuleSO`는 기존 `soundSettings`를 유지한다.
- line 설정 override는 개별 필드가 아니라 사운드 설정 블록 전체 단위다.

### 3. Batch Migration은 하지 않는다

기존 line asset을 한 번에 새 구조로 다시 쓰는 자동 마이그레이션은 하지 않는다.

기존 line 데이터는 override-on 호환 규칙으로 해석하고, 사용자가 명시적으로 정리할 때만 episode 기본값 체계로 이동시킨다.

---

## Data Model

### `StoryEpisodeSO`

새 serialized field를 추가한다.

- `StorySoundSettingsData defaultSoundSettings`

editor 상단 인스펙터에서 이 값을 `Episode Sound Defaults` 섹션으로 노출한다.

### `StoryStageLayoutModuleSO`

기존 sound 데이터는 유지한다.

- `StorySoundTrackData soundTrack`
- `StorySoundSettingsData soundSettings`

여기에 line override 여부를 나타내는 serialized bool을 추가한다.

- `bool useSoundSettingsOverride`

이 bool은 sound key 존재 여부와 별개다.

- sound key만 추가했다고 자동으로 켜지지 않는다.
- 사용자가 `This Line Sound Override`를 켤 때만 true가 된다.
- 꺼져 있을 때 line의 `soundSettings` 값은 보존되지만 런타임/에디터 해석에는 사용되지 않는다.

### Compatibility Rule

기존 asset을 안전하게 유지하기 위해 다음 호환 규칙을 둔다.

- legacy line이 아래 둘 중 하나를 만족하면 기본적으로 override-on으로 해석한다.
- `soundTrack`에 `BGM` 또는 `SFX` keyframe이 하나라도 있다.
- 저장된 line `soundSettings`가 현재 episode 기본값 해석 결과와 다르다.
- 기존 line의 `soundTrack`과 `soundSettings` 값은 유지한다.
- batch rewrite나 일괄 asset 재저장은 하지 않는다.

### Episode Default Seeding

기존 episode에는 `defaultSoundSettings`가 아직 명시적으로 작성되지 않았을 수 있다.

이 경우 editor는 다음 순서로 `Episode Sound Defaults` 초기 표시값을 결정한다.

1. 명시적으로 저장된 `episode.defaultSoundSettings`
2. episode 내 첫 번째 sound-authored line의 기존 sound settings
3. `StorySoundSettingsData`의 struct 기본값

구현은 명시 기본값과 legacy seed를 구분할 수 있어야 한다. 필요하면 explicit authored flag를 내부적으로 둘 수 있다.

---

## Runtime Resolution

런타임 sound settings 해석 순서는 다음과 같다.

1. 현재 line의 `StoryStageLayoutModuleSO`가 있고 `useSoundSettingsOverride == true`이면 `layout.soundSettings` 사용
2. 그렇지 않으면 `episode.defaultSoundSettings` 사용

기존 사운드 런타임 의미는 유지한다.

- authoring preview에서는 sound key가 실제 오디오를 재생하지 않는다.
- runtime에서 `BGM Play` key는 현재 카메라 중심 기준 위치로 이벤트를 발행한다.
- 새 line에 `BGM` key가 없으면 이전 story BGM을 유지한다.
- `BGM Stop` key는 fade-out 후 정지한다.
- `BGM Crossfade`는 이전 BGM과 새 BGM이 잠깐 겹치며 전환한다.
- `SFX`는 line 내부에서는 누적 재생되고, line 이동 시 fade-out 정리한다.
- story session 종료 시 현재 story BGM도 fade-out 후 정지한다.

이번 변경은 위 런타임 의미를 바꾸기 위한 작업이 아니다.

---

## Editor UX

### Top Inspector

상단 인스펙터에 사운드 설정을 상시 노출한다.

- `Episode Sound Defaults`
- `This Line Sound Override`

`This Line Sound Override`는 현재 line 기준으로 표시하며, line에 `Stage Layout`이 있을 때만 활성 편집된다.

`Stage Layout`이 아직 없으면 다음 액션 중 하나가 최초로 발생할 때만 생성한다.

- `Override This Line`을 켜는 순간
- `Add Property -> BGM`
- `Add Property -> SFX`
- 현재 line sound 값을 실제로 수정하는 순간

단순 line 선택이나 sound selection 진입만으로는 `Stage Layout`을 만들지 않는다.

### Sound Selection Entry

- `Select Sound` 버튼은 제거한다.
- 모든 line의 우측 리스트에 singleton `Sound` 오브젝트를 항상 표시한다.
- 사운드 타임라인 편집 진입점은 이 `Sound` 오브젝트 클릭 하나만 둔다.

`Sound` 오브젝트를 클릭하면 `StageSelectionKind.Sound`로 진입하고, 타임라인은 sound editing 모드가 된다.

### Sound Inspector

sound selection 상태의 하단/우측 인스펙터는 sound settings를 중복 노출하지 않는다.

- settings는 상단 `Episode Sound Defaults` / `This Line Sound Override`에서만 편집
- sound selection 상태 인스펙터는 선택된 sound key 정보만 표시
- 다중 선택 상태에서는 기존 트랙과 동일하게 key value 편집을 막는다

---

## Timeline Rules

### Common Rule

사운드 타임라인은 액터/배경/카메라와 동일한 규칙을 따른다.

- property row는 key가 있을 때만 존재한다.
- `Add Property`는 항상 현재 playhead 위치에 첫 key를 즉시 만든다.
- 마지막 key를 지우면 property row도 함께 사라진다.
- property type당 row는 하나만 허용한다.

사운드에서 허용되는 property row는 다음 둘이다.

- `BGM`
- `SFX`

### Add Property

`Add Property` 드롭다운은 selection 상태에 맞는 property만 보여준다.

sound selection 상태에서는 다음 규칙을 쓴다.

- `BGM` 추가 시 현재 playhead에 첫 `BGM` key 생성
- `SFX` 추가 시 현재 playhead에 첫 `SFX` key 생성
- `BgmSounds` enum이 비어 있으면 `BGM` property 추가 비활성 + 안내 표시
- `SfxSounds` enum이 비어 있으면 `SFX` property 추가 비활성 + 안내 표시

### Key Creation

모든 sound key는 항상 현재 playhead 위치에 생성된다.

`BGM` key는 하나의 key 안에서 `Play`/`Stop`을 바꾸는 방식으로 통합한다.

- `Add Property -> BGM`의 첫 key 기본값은 `Play`
- 기존 BGM row 우클릭 `Add/Select Key`의 새 key 기본값도 `Play`
- `Stop`이 필요하면 생성 후 인스펙터에서 수동 변경

`BGM`/`SFX` row 우클릭 `Add/Select Key`는 다음 규칙을 따른다.

- 같은 row, 같은 시간에 key가 없으면 새 key 생성
- 같은 row, 같은 시간에 key가 이미 있으면 새로 덮어쓰지 않고 그 key만 선택

### Selection, Drag, Delete

사운드도 공용 타임라인 선택 규칙을 그대로 따른다.

- ctrl multi-select 가능
- box select 가능
- group drag 가능
- Delete 즉시 적용
- Undo/Redo 즉시 반영

다중 선택 상태에서는 기존 타임라인과 동일하게 인스펙터에서 key value 편집을 막는다.

### Collision Rule

같은 row, 같은 시간에는 key가 항상 하나만 존재해야 한다.

이 규칙은 추가/붙여넣기/드래그 모두에 적용한다.

단일 key 드래그 충돌:

- 도착 시간의 기존 key 유지
- 움직인 key는 원래 위치로 복귀

그룹 드래그 충돌:

- 선택된 key 중 하나라도 충돌하면 그룹 전체를 원래 위치로 복귀
- 일부만 성공하는 부분 적용은 허용하지 않음

### Existing Data When Enum Becomes Empty

enum이 비어도 기존 row/key는 유지하고 표시한다.

- 새 property 생성만 비활성
- 기존 key의 `Time`, `Operation` 등은 편집 가능
- 기존 key의 `BGM`/`SFX` enum 값 필드는 비활성 + 안내 표시

---

## Undo / Redo Contract

Undo 단위는 액터/배경/카메라/사운드 전부 동일하게 맞춘다.

- 단일 key 드래그 1회 = Undo 1스텝
- 그룹 key 드래그 1회 = Undo 1스텝
- `Add Property` 1회 = Undo 1스텝
- `Add/Select Key`로 실제 key를 만든 행동 1회 = Undo 1스텝
- `Delete` 1회 = Undo 1스텝
- `Operation` 변경 1회 = Undo 1스텝

드래그 중 프레임마다 Undo가 쌓이지 않도록 공용 타임라인 저장 흐름을 정리해야 한다.

---

## Implementation Scope

### Included

- `StoryEpisodeSO`에 episode sound defaults 추가
- `StoryStageLayoutModuleSO`에 line sound override flag 추가
- `StoryRunner`에 episode default / line override 해석 반영
- `StoryPreviewWindow.Inspector.cs`에 상단 sound settings UI 추가
- `StoryPreviewWindow.Sound.cs`에서 sound key inspector 책임만 유지
- `StoryPreviewWindow.Timeline.cs`에서 sound를 공용 타임라인 interaction에 편입
- Delete / Undo / Redo / drag collision / multi-select 규칙을 공용화

### Not Included

- `SoundManager`, `SoundPlayer`, `SoundEvents`의 재설계
- story runtime 사운드 의미 변경
- 새로운 sound property 종류 추가
- sound 데이터의 범용 keyframe 모델 마이그레이션

---

## Verification

### Editor Verification

1. 모든 line의 우측 리스트에 `Sound` 오브젝트가 기본으로 보인다.
2. `Select Sound` 버튼이 제거되어 있다.
3. `Sound` 오브젝트 클릭만으로 sound timeline editing에 진입한다.
4. 상단 인스펙터에 `Episode Sound Defaults`가 항상 보인다.
5. 현재 line 기준 `This Line Sound Override`를 켜고 끌 수 있다.
6. override를 꺼도 line sound settings 값은 보존된다.
7. sound selection 상태에서 `Add Property -> BGM/SFX`가 기존 트랙과 같은 방식으로 동작한다.
8. `BGM`/`SFX` row 우클릭으로 현재 playhead에 key를 추가할 수 있다.
9. 같은 row 같은 시간에 key가 이미 있으면 새 key 생성 없이 기존 key만 선택된다.
10. sound key는 ctrl multi-select, box select, group drag가 된다.
11. 다중 선택 상태에서는 key inspector 편집이 막힌다.
12. Delete, Undo, Redo가 즉시 보인다.
13. 드래그 충돌 시 단일 key는 복귀, 그룹은 전체 복귀한다.
14. enum이 비어 있으면 새 property 생성은 막히고, 기존 row/key는 보인다.

### Runtime Verification

1. override-on line은 line sound settings를 사용한다.
2. override-off line은 episode defaults를 사용한다.
3. 기존 authored line의 BGM/SFX 동작이 회귀하지 않는다.
4. authoring preview에서는 sound key가 실제로 재생되지 않는다.
