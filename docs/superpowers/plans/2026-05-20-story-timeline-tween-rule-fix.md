# Story Timeline Tween Rule Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keyframe easing를 "arriving key owned" 모델로 전환하고, None easing = snap-at-key-time을 추가하고, single key의 line-start → key segment를 표시/편집 가능하게 하고, Import Previous Stage의 다중 predecessor 지원을 추가한다.

**Architecture:**
- `StoryStageMoveMotionType` 에 `None` 값 추가 (기존 int 값 보존, None=7)
- `StoryTransitionSampler` 샘플러를 "arriving key easing" 모델로 재구성 (pre-first-key segment base-state fallback 포함)
- `StoryPreviewWindow.Timeline.cs` 에서 segment UI를 arriving key 기준으로 변경, pre-first segment 시각화 추가
- `StoryPreviewWindow.Inspector.cs` 에서 Import Previous Stage의 다중 predecessor 선택 UI 추가

**Tech Stack:** Unity C#, UIElements (UGUI), ScriptableObject, Editor-only UIToolkit partial class

---

## 사전 분석 요약

### 현재 구조 (작업 전 이해 필수)

**1. Data**
- `StoryActorKeyframeData.easing` : 기본값 `EaseInOut` (버그: None이어야 함)
- `StoryStageMoveMotionType` : `Instant(0), Linear(1), EaseIn(2), EaseOut(3), EaseInOut(4), SmoothStep(5), SmootherStep(6)` — None 없음
- 모든 track (actor / camera / background) 이 동일한 `StoryActorKeyframeData` 사용

**2. Sampler (`StoryTransitionSampler`)**
- `TryFindSegment`: `time ≤ keys[0].time` → `from=to=keys[0]` 반환 → **첫 key 이전에도 key 값을 즉시 적용 (BUG)**
- `ResolveOutgoingEasing(track, from)` → `from.easing` 사용 (outgoing 모델, **BUG**)
- Camera/Background : `from.easing` 직접 사용 (outgoing, **BUG**)

**3. Timeline UI**
- `BuildTimelineKeyframeRow` : `for (i < indexes.Count - 1)` → key-key 사이 segment만 그림, **pre-first segment 없음 (BUG)**
- `AddSegmentBar` : `from.easing` 로 색상/레이블 표시 (outgoing, **BUG**)
- `_selectedTimelineSegmentKeyIndex` : 현재 FROM key index 저장 → 변경 후 TO key index 저장
- `BuildSelectedTimelineSegmentInspector` : `from.easing` 표시/편집 (outgoing, **BUG**)
- `ValidateTimelineSelection` : `FindNextKeyIndex` 필요 → single key는 항상 invalid

**4. Import Previous Stage**
- `TryGetPreviousLine` : 단일 predecessor만 반환
- `OnImportPreviousStageClicked` : 이미 track 클리어 + final state만 복사 ✅
- 문제: 분기(multiple predecessors)가 있으면 선택 UI 없음

### 이번 턴 리스크
1. **Easing 방향 전환** → 기존 다중 key 데이터의 애니메이션이 바뀜 (의도된 변경)
2. **`from=null` 반환** → 모든 calller가 null 처리 필요. 누락 시 NullReferenceException
3. **`_selectedTimelineSegmentKeyIndex` 의미 변경** → 7+ 사용처 일관 수정 필요
4. **편집기 전용 코드** → 컴파일 에러만 없으면 런타임 안전 (but 런타임도 sampler를 공유)
5. **None 기본값** → 기존 데이터는 EaseInOut 유지 (안전), 신규 key만 None

---

## 파일 구조

| 파일 | 역할 | 변경 내용 |
|------|------|----------|
| `StoryActorMotionProfileData.cs` | `StoryStageMoveMotionType` enum | `None = 7` 추가 |
| `StoryActorKeyframeData.cs` | 키프레임 데이터 | `easing` 기본값 `EaseInOut` → `None` |
| `StoryTransitionSampler.cs` | 공통 sampler (Editor + Runtime) | arriving-key 모델, None 처리, pre-first base-state |
| `StoryPreviewWindow.Timeline.cs` | Timeline UI partial class | segment 색상/선택 = TO key, pre-first segment 시각화 |
| `StoryPreviewWindow.Inspector.cs` | Inspector partial class | Import Previous Stage multi-predecessor 지원 |

---

## Task 1: `None` 열거값 추가 + 기본 easing 변경

**Files:**
- Modify: `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Modules/StoryActorMotionProfileData.cs`
- Modify: `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Modules/StoryActorKeyframeData.cs`

- [ ] **Step 1: `StoryStageMoveMotionType`에 `None = 7` 추가**

`StoryActorMotionProfileData.cs`의 enum:
```csharp
public enum StoryStageMoveMotionType
{
    Instant,       // 0: 즉시 TO 값으로 이동 (segment 진입 즉시)
    Linear,        // 1
    EaseIn,        // 2
    EaseOut,       // 3
    EaseInOut,     // 4
    SmoothStep,    // 5
    SmootherStep,  // 6
    None,          // 7: key time 이전은 FROM 값 유지, key time에 snap
}
```

- [ ] **Step 2: `StoryActorKeyframeData.easing` 기본값 변경**

```csharp
[Tooltip("Easing for the segment arriving at this keyframe (from previous key or line start state).")]
public StoryStageMoveMotionType easing = StoryStageMoveMotionType.None;
```

- [ ] **Step 3: Unity 컴파일 확인**

Unity Editor에서 Console 열기 → 컴파일 에러 없음 확인 (`read_console` 또는 수동).

- [ ] **Step 4: Commit**

```bash
git add "Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Modules/StoryActorMotionProfileData.cs"
git add "Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Modules/StoryActorKeyframeData.cs"
git commit -m "feat: add None easing type and set as default for new keyframes"
```

---

## Task 2: Sampler — Arriving Key 모델 + None 처리 + Pre-first Base-state

**Files:**
- Modify: `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Shared/StoryTransitionSampler.cs`

이 Task는 가장 핵심이다. 변경 순서를 지켜야 컴파일 에러가 없다.

- [ ] **Step 1: `ResolveMoveProgress`에 `None` case 추가**

기존 `ResolveMoveProgress` 메서드의 switch 앞에 다음을 추가:
```csharp
public static float ResolveMoveProgress(StoryStageMoveMotionType motion, float duration, float elapsed)
{
    if (motion == StoryStageMoveMotionType.Instant || duration <= 0f)
        return 1f;

    // None: hold FROM value until key time, then snap
    if (motion == StoryStageMoveMotionType.None)
    {
        float t = Mathf.Clamp01(elapsed / duration);
        return t >= 1f ? 1f : 0f;
    }

    float tt = Mathf.Clamp01(elapsed / duration);
    return motion switch
    {
        StoryStageMoveMotionType.EaseIn        => tt * tt * tt,
        StoryStageMoveMotionType.EaseOut       => 1f - Mathf.Pow(1f - tt, 3f),
        StoryStageMoveMotionType.EaseInOut     => Mathf.SmoothStep(0f, 1f, tt),
        StoryStageMoveMotionType.SmoothStep    => Mathf.SmoothStep(0f, 1f, tt),
        StoryStageMoveMotionType.SmootherStep  => tt * tt * tt * (tt * (6f * tt - 15f) + 10f),
        _                                      => tt
    };
}
```

- [ ] **Step 2: `TryFindSegment` 변경 — pre-first case에서 `from=null` 반환**

완전 교체:
```csharp
private static bool TryFindSegment(
    StoryActorTrackData track,
    StoryActorKeyframeProperty property,
    float time,
    out StoryActorKeyframeData from,
    out StoryActorKeyframeData to,
    out float local)
{
    from = null;
    to = null;
    local = 0f;

    var keys = new List<StoryActorKeyframeData>();
    foreach (StoryActorKeyframeData keyframe in track.keyframes)
    {
        if (keyframe != null && keyframe.property == property)
            keys.Add(keyframe);
    }

    keys.Sort((a, b) => GetKeyTime(a).CompareTo(GetKeyTime(b)));
    if (keys.Count == 0)
        return false;

    float firstKeyTime = GetKeyTime(keys[0]);

    // Pre-first segment: time <= first key time
    if (time <= firstKeyTime)
    {
        from = null; // sentinel: use base state as origin
        to = keys[0];
        local = firstKeyTime <= 0f ? 1f : Mathf.Clamp01(time / firstKeyTime);
        return true;
    }

    // At or past last key: hold last key value
    StoryActorKeyframeData last = keys[keys.Count - 1];
    if (time >= GetKeyTime(last))
    {
        from = last;
        to = last;
        return true;
    }

    // Find bracket [from, to] where from.time <= time < to.time
    from = keys[0];
    to = last;
    for (int i = 1; i < keys.Count; i++)
    {
        if (GetKeyTime(keys[i]) >= time)
        {
            to = keys[i];
            break;
        }
        from = keys[i];
    }

    float fromTime = GetKeyTime(from);
    float toTime = GetKeyTime(to);
    local = Mathf.Clamp01((time - fromTime) / Mathf.Max(0.0001f, toTime - fromTime));
    return true;
}
```

- [ ] **Step 3: `ResolveOutgoingEasing` 를 `ResolveArrivingEasing` 으로 교체**

기존 `ResolveOutgoingEasing` 제거, 새 메서드 추가:
```csharp
// Arriving key owns the easing for the segment leading to it.
// Also checks for a separate Easing-property keyframe at the same time.
private static StoryStageMoveMotionType ResolveArrivingEasing(StoryActorTrackData track, StoryActorKeyframeData to)
{
    float time = GetKeyTime(to);
    foreach (StoryActorKeyframeData keyframe in track.keyframes)
    {
        if (keyframe != null
            && keyframe.property == StoryActorKeyframeProperty.Easing
            && Mathf.Approximately(GetKeyTime(keyframe), time))
            return keyframe.easing;
    }
    return to.easing;
}
```

- [ ] **Step 4: `TrySampleVector2` 변경 — base-state fallback + arriving easing**

완전 교체 (signature에 `Vector2 baseValue` 추가):
```csharp
private static bool TrySampleVector2(
    StoryActorTrackData track,
    StoryActorKeyframeProperty property,
    float time,
    Func<StoryActorKeyframeData, Vector2> selector,
    Vector2 baseValue,
    out Vector2 value)
{
    value = default;
    if (!TryFindSegment(track, property, time, out var from, out var to, out float local))
        return false;

    if (to == null) return false;

    // from==to: at or past last key, or (from==null && local==1)
    if (from == to)
    {
        value = selector(from);
        return true;
    }

    StoryStageMoveMotionType easing = ResolveArrivingEasing(track, to);
    float progress = ResolveMoveProgress(easing, 1f, local);

    // from==null: pre-first segment, use base state as origin
    Vector2 fromValue = from != null ? selector(from) : baseValue;
    value = Vector2.LerpUnclamped(fromValue, selector(to), progress);
    return true;
}
```

- [ ] **Step 5: `TrySampleFloat` 변경 — base-state fallback + arriving easing**

완전 교체:
```csharp
private static bool TrySampleFloat(
    StoryActorTrackData track,
    StoryActorKeyframeProperty property,
    float time,
    Func<StoryActorKeyframeData, float> selector,
    float baseValue,
    out float value)
{
    value = default;
    if (!TryFindSegment(track, property, time, out var from, out var to, out float local))
        return false;

    if (to == null) return false;

    if (from == to)
    {
        value = selector(from);
        return true;
    }

    StoryStageMoveMotionType easing = ResolveArrivingEasing(track, to);
    float progress = ResolveMoveProgress(easing, 1f, local);

    float fromValue = from != null ? selector(from) : baseValue;
    value = Mathf.LerpUnclamped(fromValue, selector(to), progress);
    return true;
}
```

- [ ] **Step 6: `SampleActorTrackAtTime` 업데이트 — baseValue 전달**

```csharp
public static StoryActorStateData SampleActorTrackAtTime(
    StoryActorStateData baseState,
    StoryActorTrackData track,
    float timeSeconds)
{
    if (baseState == null || track == null || track.keyframes == null || track.keyframes.Count == 0)
        return baseState != null ? baseState.ShallowClone() : null;

    StoryActorStateData sample = baseState.ShallowClone();
    if (sample.focusVisualAlpha < 0f)
        sample.focusVisualAlpha = ResolveFocusAlpha(sample.focused);
    float t = Mathf.Max(0f, timeSeconds);

    if (TrySampleVector2(track, StoryActorKeyframeProperty.Position, t,
            k => k.stageLocalPosition, baseState.stageLocalPosition, out Vector2 position))
        sample.stageLocalPosition = position;

    if (TrySampleFloat(track, StoryActorKeyframeProperty.Scale, t,
            k => k.scale.y, baseState.scaleMultiplier, out float scaleY))
        sample.scaleMultiplier = Mathf.Max(0.001f, scaleY);

    if (TrySampleExpression(track, t, out var expression))
    {
        sample.expression = expression;
        sample.expressionKey = string.Empty;
    }

    return sample;
}
```

- [ ] **Step 7: `TryFindCameraSegment` 변경 — pre-first `from=null`**

`TryFindBackgroundSegment`와 구조 동일. 완전 교체:
```csharp
private static bool TryFindCameraSegment(
    StoryCameraTrackData track,
    StoryActorKeyframeProperty property,
    float time,
    out StoryActorKeyframeData from,
    out StoryActorKeyframeData to,
    out float local)
{
    from = null;
    to = null;
    local = 0f;

    if (track == null || track.keyframes == null)
        return false;

    var keys = new List<StoryActorKeyframeData>();
    foreach (StoryActorKeyframeData keyframe in track.keyframes)
    {
        if (keyframe != null && keyframe.property == property)
            keys.Add(keyframe);
    }

    keys.Sort((a, b) => GetKeyTime(a).CompareTo(GetKeyTime(b)));
    if (keys.Count == 0)
        return false;

    float firstKeyTime = GetKeyTime(keys[0]);

    if (time <= firstKeyTime)
    {
        from = null;
        to = keys[0];
        local = firstKeyTime <= 0f ? 1f : Mathf.Clamp01(time / firstKeyTime);
        return true;
    }

    StoryActorKeyframeData last = keys[keys.Count - 1];
    if (time >= GetKeyTime(last))
    {
        from = last;
        to = last;
        return true;
    }

    from = keys[0];
    to = last;
    for (int i = 1; i < keys.Count; i++)
    {
        if (GetKeyTime(keys[i]) >= time)
        {
            to = keys[i];
            break;
        }
        from = keys[i];
    }

    float fromTime = GetKeyTime(from);
    float toTime = GetKeyTime(to);
    local = Mathf.Clamp01((time - fromTime) / Mathf.Max(0.0001f, toTime - fromTime));
    return true;
}
```

- [ ] **Step 8: `TrySampleCameraVector2` / `TrySampleCameraFloat` 변경 — arriving easing + base-state**

```csharp
private static bool TrySampleCameraVector2(
    StoryCameraTrackData track,
    StoryActorKeyframeProperty property,
    float time,
    Func<StoryActorKeyframeData, Vector2> selector,
    Vector2 baseValue,
    out Vector2 value)
{
    value = default;
    if (!TryFindCameraSegment(track, property, time, out var from, out var to, out float local))
        return false;

    if (to == null) return false;
    if (from == to) { value = selector(from); return true; }

    float progress = ResolveMoveProgress(to.easing, 1f, local);
    Vector2 fromValue = from != null ? selector(from) : baseValue;
    value = Vector2.LerpUnclamped(fromValue, selector(to), progress);
    return true;
}

private static bool TrySampleCameraFloat(
    StoryCameraTrackData track,
    StoryActorKeyframeProperty property,
    float time,
    Func<StoryActorKeyframeData, float> selector,
    float baseValue,
    out float value)
{
    value = default;
    if (!TryFindCameraSegment(track, property, time, out var from, out var to, out float local))
        return false;

    if (to == null) return false;
    if (from == to) { value = selector(from); return true; }

    float progress = ResolveMoveProgress(to.easing, 1f, local);
    float fromValue = from != null ? selector(from) : baseValue;
    value = Mathf.LerpUnclamped(fromValue, selector(to), progress);
    return true;
}
```

- [ ] **Step 9: `SampleCameraTrackAtTime` 업데이트 — baseValue 전달**

```csharp
if (TrySampleCameraVector2(track, StoryActorKeyframeProperty.CameraOffset, t,
        k => k.cameraStageLocalPosition, sample.stageLocalPosition, out Vector2 camPos))
    sample.stageLocalPosition = camPos;

if (TrySampleCameraFloat(track, StoryActorKeyframeProperty.CameraZoom, t,
        k => k.cameraZoom, sample.zoom, out float zoom))
    sample.zoom = Mathf.Max(0.01f, zoom);
```
(나머지 logic 동일 유지)

- [ ] **Step 10: `TryFindBackgroundSegment` 변경**

`TryFindCameraSegment`와 동일 패턴으로 `StoryBackgroundTrackData`용으로 교체.

- [ ] **Step 11: `TrySampleBackgroundVector2` / `TrySampleBackgroundFloat` 변경**

Camera 버전과 동일 패턴, `StoryBackgroundTrackData` 타입 사용.

- [ ] **Step 12: `SampleBackgroundTrackAtTime` 업데이트 — baseValue 전달**

```csharp
if (TrySampleBackgroundVector2(track, StoryActorKeyframeProperty.BackgroundPosition, t,
        k => k.stageLocalPosition, sample.stageLocalPosition, out Vector2 pos))
    sample.stageLocalPosition = pos;

if (TrySampleBackgroundFloat(track, StoryActorKeyframeProperty.BackgroundScale, t,
        k => k.scale.y, sample.scaleMultiplier, out float bgScale))
    sample.scaleMultiplier = Mathf.Max(0.001f, bgScale);
```

- [ ] **Step 13: Unity 컴파일 확인**

Console에 컴파일 에러 없음 확인.

- [ ] **Step 14: Commit**

```bash
git add "Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Shared/StoryTransitionSampler.cs"
git commit -m "fix: sampler arriving-key easing model, None snap behavior, pre-first base-state segment"
```

---

## Task 3: Timeline UI — Arriving Key 표시 + Pre-first Segment + Inspector 수정

**Files:**
- Modify: `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Editor/StoryPreviewWindow.Timeline.cs`

이 Task에서 `_selectedTimelineSegmentKeyIndex` 의미를 **FROM key index → TO (arriving) key index** 로 변경한다.

### Step 1: `FindPreviousKeyIndex` 헬퍼 추가

Timeline.cs 하단에 추가:
```csharp
private static int FindPreviousKeyIndex(
    IReadOnlyList<StoryActorKeyframeData> keyframes,
    int keyIndex,
    StoryActorKeyframeProperty property)
{
    if (keyframes == null || keyIndex < 0 || keyIndex >= keyframes.Count)
        return -1;

    float keyTime = StoryTransitionSampler.GetKeyTime(keyframes[keyIndex]);
    int prevIndex = -1;
    float prevTime = -1f;
    for (int i = 0; i < keyframes.Count; i++)
    {
        if (i == keyIndex) continue;
        StoryActorKeyframeData k = keyframes[i];
        if (k == null || k.property != property) continue;
        float t = StoryTransitionSampler.GetKeyTime(k);
        if (t < keyTime && t > prevTime) { prevTime = t; prevIndex = i; }
    }
    return prevIndex;
}
```

- [ ] **Step 2: `HandleSegmentPointerDown` signature 변경 — `toIndex` 추가**

```csharp
private void HandleSegmentPointerDown(
    PointerDownEvent e,
    StoryActorKeyframeData from,   // null for pre-first segment
    StoryActorKeyframeData to,
    int fromIndex,                  // -1 for pre-first segment
    int toIndex)
{
    if (e.button != 0 && e.button != 1)
        return;

    _timelinePanel?.Focus();
    if (from != null && ShouldStartTimelineGroupDragFromSegment(e, from, to))
    {
        BeginTimelineGroupKeyDrag(e);
        e.StopPropagation();
        return;
    }

    SelectTimelineSegment(toIndex, to.property);  // store TO key index
    _timelinePlayheadTime = from != null
        ? StoryTransitionSampler.GetKeyTime(from)
        : 0f;
    ApplyTimelinePlayheadSample();
    if (e.button == 1)
        ShowEasingMenu(toIndex, to.property);  // easing on TO key
    else
        RefreshTimelinePanel();
    RefreshActorInspector();
    e.StopPropagation();
}
```

- [ ] **Step 3: `AddSegmentBar` 변경 — TO key 기준으로 색상/선택/등록**

```csharp
private void AddSegmentBar(VisualElement lane, IReadOnlyList<StoryActorKeyframeData> keyframes, int fromIndex, int toIndex)
{
    StoryActorKeyframeData from = keyframes[fromIndex];
    StoryActorKeyframeData to = keyframes[toIndex];
    float fromX = StoryTransitionSampler.GetKeyTime(from) * _timelinePixelsPerSecond;
    float toX = StoryTransitionSampler.GetKeyTime(to) * _timelinePixelsPerSecond;
    float width = Mathf.Max(2f, toX - fromX);

    // Selected = TO key matches current selection
    bool selected = _selectedTimelineSegmentKeyIndex == toIndex
        && _selectedTimelineSegmentProperty == to.property;

    Color segmentColor = ResolveEasingColor(to.easing);  // arriving key's easing

    var hitArea = new VisualElement
    {
        tooltip = $"{to.property} segment [{StoryTransitionSampler.GetKeyTime(from):0.00}s → {to.easing}]",
        style =
        {
            position = Position.Absolute,
            left = fromX,
            top = (TimelineRowHeight - TimelineSegmentHitHeight) * 0.5f,
            width = width,
            height = TimelineSegmentHitHeight,
            backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.001f))
        }
    };
    hitArea.RegisterCallback<PointerDownEvent>(e => HandleSegmentPointerDown(e, from, to, fromIndex, toIndex));
    lane.Add(hitArea);

    float visualHeight = selected ? TimelineSelectedSegmentHeight : TimelineSegmentHeight;
    var bar = new VisualElement
    {
        tooltip = $"{to.property} segment [{StoryTransitionSampler.GetKeyTime(from):0.00}s → {to.easing}]",
        style =
        {
            position = Position.Absolute,
            left = fromX,
            top = TimelineRowHeight * 0.5f - visualHeight * 0.5f,
            width = width,
            height = visualHeight,
            backgroundColor = new StyleColor(selected ? Color.Lerp(segmentColor, new Color(1f, 0.60f, 0.18f, 0.95f), 0.45f) : segmentColor),
            borderTopWidth = selected ? 1 : 0,
            borderBottomWidth = selected ? 1 : 0,
            borderTopColor = new StyleColor(new Color(1f, 0.88f, 0.30f, 0.95f)),
            borderBottomColor = new StyleColor(new Color(1f, 0.88f, 0.30f, 0.95f))
        }
    };
    bar.RegisterCallback<PointerDownEvent>(e => HandleSegmentPointerDown(e, from, to, fromIndex, toIndex));
    lane.Add(bar);

    if (width >= 56f)
    {
        lane.Add(new Label(ShortEasingLabel(to.easing))
        {
            pickingMode = PickingMode.Ignore,
            style =
            {
                position = Position.Absolute,
                left = fromX + width * 0.5f - 38f,
                top = 1,
                width = 76,
                height = 12,
                fontSize = 8,
                unityTextAlign = TextAnchor.MiddleCenter,
                color = new StyleColor(selected ? new Color(1f, 0.84f, 0.38f) : ResolveEasingLabelColor(to.easing))
            }
        });
    }
}
```

- [ ] **Step 4: `AddPreFirstSegmentBar` 신규 메서드 추가**

`AddSegmentBar` 아래에 추가:
```csharp
private void AddPreFirstSegmentBar(VisualElement lane, IReadOnlyList<StoryActorKeyframeData> keyframes, int firstKeyIndex)
{
    StoryActorKeyframeData firstKey = keyframes[firstKeyIndex];
    float firstKeyTime = StoryTransitionSampler.GetKeyTime(firstKey);
    if (firstKeyTime <= 0.001f) return; // zero-time key: no pre-first segment to show

    float fromX = 0f;
    float toX = firstKeyTime * _timelinePixelsPerSecond;
    float width = Mathf.Max(2f, toX - fromX);

    bool selected = _selectedTimelineSegmentKeyIndex == firstKeyIndex
        && _selectedTimelineSegmentProperty == firstKey.property;

    // Dimmer color to indicate "from line start"
    Color baseColor = ResolveEasingColor(firstKey.easing);
    Color segmentColor = Color.Lerp(baseColor, new Color(0.25f, 0.25f, 0.27f, 0.85f), 0.45f);

    var hitArea = new VisualElement
    {
        tooltip = $"{firstKey.property} segment [Line Start → {firstKey.easing}]",
        style =
        {
            position = Position.Absolute,
            left = fromX,
            top = (TimelineRowHeight - TimelineSegmentHitHeight) * 0.5f,
            width = width,
            height = TimelineSegmentHitHeight,
            backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.001f))
        }
    };
    // from=null, fromIndex=-1 to indicate pre-first
    hitArea.RegisterCallback<PointerDownEvent>(e =>
        HandleSegmentPointerDown(e, null, firstKey, -1, firstKeyIndex));
    lane.Add(hitArea);

    float visualHeight = selected ? TimelineSelectedSegmentHeight : TimelineSegmentHeight;
    var bar = new VisualElement
    {
        tooltip = $"{firstKey.property} segment [Line Start → {firstKey.easing}]",
        style =
        {
            position = Position.Absolute,
            left = fromX,
            top = TimelineRowHeight * 0.5f - visualHeight * 0.5f,
            width = width,
            height = visualHeight,
            backgroundColor = new StyleColor(selected
                ? Color.Lerp(segmentColor, new Color(1f, 0.60f, 0.18f, 0.95f), 0.45f)
                : segmentColor),
            borderTopWidth = selected ? 1 : 0,
            borderBottomWidth = selected ? 1 : 0,
            borderTopColor = new StyleColor(new Color(1f, 0.88f, 0.30f, 0.95f)),
            borderBottomColor = new StyleColor(new Color(1f, 0.88f, 0.30f, 0.95f))
        }
    };
    bar.RegisterCallback<PointerDownEvent>(e =>
        HandleSegmentPointerDown(e, null, firstKey, -1, firstKeyIndex));
    lane.Add(bar);

    if (width >= 56f)
    {
        lane.Add(new Label(ShortEasingLabel(firstKey.easing))
        {
            pickingMode = PickingMode.Ignore,
            style =
            {
                position = Position.Absolute,
                left = fromX + width * 0.5f - 38f,
                top = 1,
                width = 76,
                height = 12,
                fontSize = 8,
                unityTextAlign = TextAnchor.MiddleCenter,
                color = new StyleColor(selected
                    ? new Color(1f, 0.84f, 0.38f)
                    : ResolveEasingLabelColor(firstKey.easing))
            }
        });
    }
}
```

- [ ] **Step 5: `BuildTimelineKeyframeRow` 변경 — pre-first segment 추가**

```csharp
private void BuildTimelineKeyframeRow(string label, IReadOnlyList<StoryActorKeyframeData> keyframes, StoryActorKeyframeProperty property)
{
    var row = CreateTimelineRow(label);
    float duration = Mathf.Max(2f, GetTimelineDuration() + 0.5f, _timelinePlayheadTime + 0.5f);
    var lane = CreateTimelineLane(duration, TimelineRowHeight, property);

    var indexes = GetOrderedKeyIndexes(keyframes, property);
    if (SupportsTimelineSegment(property))
    {
        // Pre-first segment: from line start (t=0) to first key, when first key time > 0
        if (indexes.Count > 0)
            AddPreFirstSegmentBar(lane, keyframes, indexes[0]);

        // Segments between consecutive keys
        for (int i = 0; i < indexes.Count - 1; i++)
            AddSegmentBar(lane, keyframes, indexes[i], indexes[i + 1]);
    }

    foreach (int keyIndex in indexes)
        AddActorKeyMarker(lane, keyIndex, keyframes[keyIndex], property);

    AddTimelinePlayhead(lane, duration, TimelineRowHeight);
    row.Add(lane);
    _timelineRows.Add(row);
}
```

- [ ] **Step 6: `ValidateTimelineSelection` 변경 — TO key semantics**

현재 segment validation 로직 교체 (Timeline.cs 내 `ValidateTimelineSelection`에서 segment 부분):
```csharp
// OLD logic (requires a next key):
// if (_selectedTimelineSegmentKeyIndex >= 0 && (... || FindNextKeyIndex(...) < 0)) { reset; }

// NEW: TO key is valid if it exists and has the right property + supports segment
if (_selectedTimelineSegmentKeyIndex >= 0
    && (_selectedTimelineSegmentKeyIndex >= keyframes.Count
        || keyframes[_selectedTimelineSegmentKeyIndex] == null
        || keyframes[_selectedTimelineSegmentKeyIndex].property != _selectedTimelineSegmentProperty
        || !SupportsTimelineSegment(_selectedTimelineSegmentProperty)))
{
    _selectedTimelineSegmentKeyIndex = -1;
}
```

- [ ] **Step 7: `ShowEasingMenu` 확인 — TO key로 동작하는지 체크**

`ShowEasingMenu(int keyIndex, ...)` 는 `keyframes[keyIndex].easing`을 읽고 씀.
이제 `keyIndex`가 TO key를 가리키므로 추가 변경 불필요.
코드 확인만:
```csharp
private void ShowEasingMenu(int keyIndex, StoryActorKeyframeProperty property)
{
    // keyIndex = TO key index (arriving key) — already correct after Task 3 changes
    // ... existing code: reads/writes keyframes[keyIndex].easing
}
```

- [ ] **Step 8: `BuildSelectedTimelineSegmentInspector` 변경 — TO key 기준으로 easing 표시**

`StoryPreviewWindow.Inspector.cs` 에 있음. 완전 교체:
```csharp
private void BuildSelectedTimelineSegmentInspector()
{
    if (_selectedTimelineSegmentKeyIndex < 0)
        return;

    IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
    if (keyframes == null
        || _selectedTimelineSegmentKeyIndex >= keyframes.Count
        || keyframes[_selectedTimelineSegmentKeyIndex] == null)
        return;

    int toIndex = _selectedTimelineSegmentKeyIndex;
    StoryActorKeyframeData toKey = keyframes[toIndex];
    StoryActorKeyframeData segmentKeyRef = toKey;

    // Find from-side for display label only
    int fromIndex = FindPreviousKeyIndex(keyframes, toIndex, _selectedTimelineSegmentProperty);
    float fromTime = fromIndex >= 0 ? StoryTransitionSampler.GetKeyTime(keyframes[fromIndex]) : 0f;
    string fromLabel = fromIndex >= 0 ? $"{fromTime:0.00}s" : "Line Start";
    float toTime = StoryTransitionSampler.GetKeyTime(toKey);

    _inspectorRoot.Add(MakeSeparator());
    _inspectorRoot.Add(MakeBoldLabel("Selected Segment"));
    _inspectorRoot.Add(new Label($"{toKey.property}: {fromLabel} → {toTime:0.00}s")
    {
        style = { marginBottom = 4, color = new StyleColor(new Color(0.70f, 0.72f, 0.78f)) }
    });

    var easingField = new EnumField("Easing", toKey.easing)
    {
        style = { marginBottom = 3 }
    };
    easingField.RegisterValueChangedCallback(e =>
    {
        SaveCurrentTimelineKeyframes(currentKeys =>
        {
            int index = currentKeys.IndexOf(segmentKeyRef);
            if (index >= 0)
                currentKeys[index].easing = (StoryStageMoveMotionType)e.newValue;
        }, refresh: false);
        ApplyTimelinePlayheadSample();
        RefreshTimelinePanel();
    });
    _inspectorRoot.Add(easingField);
}
```

- [ ] **Step 9: Unity 컴파일 확인**

Console 에러 없음 확인. 특히 `FindPreviousKeyIndex` 가 Timeline.cs 파일 안에 있고 Inspector.cs 에서 호출하려면 같은 partial class 안에 있어야 한다.
→ `FindPreviousKeyIndex` 는 `StoryPreviewWindow.Timeline.cs` 에만 추가하면 된다.
→ `BuildSelectedTimelineSegmentInspector` 은 `StoryPreviewWindow.Inspector.cs` 에 있으므로 같은 partial class이므로 호출 가능.

- [ ] **Step 10: Commit**

```bash
git add "Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Editor/StoryPreviewWindow.Timeline.cs"
git add "Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Editor/StoryPreviewWindow.Inspector.cs"
git commit -m "fix: timeline UI arriving-key easing, pre-first segment visualization, segment inspector TO key"
```

---

## Task 4: Import Previous Stage — Multi-predecessor 지원

**Files:**
- Modify: `Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Editor/StoryPreviewWindow.Inspector.cs`

- [ ] **Step 1: `GetAllPreviousLineCandidates` 메서드 추가**

Inspector.cs의 `TryGetPreviousLine` 호출부 근처 (하단)에 추가:
```csharp
private List<StoryLineSO> GetAllPreviousLineCandidates(StoryLineSO line)
{
    var candidates = new List<StoryLineSO>();
    if (line == null || episode == null)
        return candidates;

    // Primary: find all lines whose NextLineId == current line's LineId
    foreach (StoryLineSO candidate in episode.Lines)
    {
        if (candidate != null && candidate.NextLineId == line.LineId)
            candidates.Add(candidate);
    }

    // Fallback: no explicit link → use linear episode order
    if (candidates.Count == 0)
    {
        int index = FindLineIndex(line);
        if (index > 0 && episode.Lines[index - 1] != null)
            candidates.Add(episode.Lines[index - 1]);
    }

    return candidates;
}
```

- [ ] **Step 2: `ApplyImportPreviousStage` 분리**

`OnImportPreviousStageClicked` 내부 로직을 별도 메서드로 추출:
```csharp
private void ApplyImportPreviousStage(StoryLineSO previousLine)
{
    if (!TryBuildFinalStageStateAtLine(previousLine, out var previousActors, out var previousBackground, out var previousCamera))
        return;

    if (previousActors.Count == 0 && previousBackground == null && IsDefaultCameraState(previousCamera))
        return;

    StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Import Previous Stage");
    if (layout == null)
        return;

    bool hasExistingData = layout.ActorsEditable.Count > 0
        || layout.BackgroundTrackEditable.keyframes.Count > 0
        || layout.ActorTracksEditable.Count > 0
        || layout.CameraTrackEditable.keyframes.Count > 0;

    if (hasExistingData)
    {
        bool overwrite = EditorUtility.DisplayDialog(
            "Import Previous Stage",
            "Overwrite the current line snapshot with the previous line's final static result and clear current actor/background/camera tracks?",
            "Overwrite",
            "Cancel");
        if (!overwrite)
            return;
    }

    Undo.RecordObject(layout, "Import Previous Stage");
    layout.ActorsEditable.Clear();
    layout.ActorTracksEditable.Clear();
    layout.BackgroundTrackEditable.keyframes.Clear();
    layout.CameraTrackEditable.keyframes.Clear();

    foreach (var pair in previousActors)
    {
        StoryActorStateData clone = pair.Value.ShallowClone();
        clone.EnsureActorInstanceKey(pair.Key);
        clone.SyncActorKey();
        layout.ActorsEditable.Add(clone);
    }

    CopyBackgroundState(previousBackground, layout.BackgroundEditable);
    CopyCameraState(previousCamera, layout.CameraTrackEditable.defaultState);
    layout.CameraFocusTargetEditable = previousCamera?.targetActorInstanceKey ?? "";
    SaveLayoutAndRefresh(layout);
}
```

- [ ] **Step 3: `OnImportPreviousStageClicked` 변경 — multi-predecessor 지원**

```csharp
private void OnImportPreviousStageClicked()
{
    if (!IsStageAuthoringMode || _currentLine == null)
        return;

    List<StoryLineSO> candidates = GetAllPreviousLineCandidates(_currentLine);
    if (candidates.Count == 0)
        return;

    if (candidates.Count == 1)
    {
        ApplyImportPreviousStage(candidates[0]);
        return;
    }

    // Multiple predecessors: show selection menu
    var menu = new GenericMenu();
    foreach (StoryLineSO candidate in candidates)
    {
        StoryLineSO captured = candidate;
        string speaker = candidate.Speaker != null ? candidate.Speaker.DisplayName : "—";
        string preview = !string.IsNullOrWhiteSpace(candidate.DialogueText)
            ? candidate.DialogueText.Substring(0, Mathf.Min(30, candidate.DialogueText.Length))
            : "(no text)";
        string label = $"{candidate.LineId} [{speaker}]: {preview}";
        menu.AddItem(new GUIContent(label), false, () => ApplyImportPreviousStage(captured));
    }
    menu.ShowAsContext();
}
```

> **주의**: `StoryLineSO`에 `Speaker`, `DialogueText` public 프로퍼티가 있는지 확인 필요.
> `StoryLineSO.cs` 확인 결과:
> - `speaker` 필드 private → `Speaker` 프로퍼티 또는 `speaker` 직접 접근 가능한지 체크.
> - 없으면: `candidate.LineId` 만으로 label 구성.

실제 사용 가능한 프로퍼티를 확인하고 label 문자열을 조정. 최소한 `LineId`는 항상 사용 가능.

- [ ] **Step 4: `RefreshAuthoringControls` 업데이트 — multi-predecessor 고려**

기존 `hasPreviousStage` 판단 로직 유지 (이미 `TryBuildStageStateBeforeLine` 사용). 추가로:
```csharp
bool hasPreviousStage = hasLine && GetAllPreviousLineCandidates(_currentLine).Count > 0;
```
(기존 actor/background 유무 체크보다 단순하게 — candidate가 있으면 버튼 활성화)

단, 이 변경이 너무 넓게 버튼을 활성화할 수 있으니, 기존 로직을 안전하게 유지하려면 기존 체크를 유지:
```csharp
bool hasPreviousStage = hasLine 
    && GetAllPreviousLineCandidates(_currentLine).Count > 0
    && TryBuildStageStateBeforeLine(_currentLine, out var pa, out var pb)
    && (pa.Count > 0 || pb != null);
```

- [ ] **Step 5: Unity 컴파일 확인 + `StoryLineSO` 프로퍼티 체크**

`StoryLineSO.cs`에서 `Speaker`, `DialogueText` 프로퍼티 이름 확인.
없으면 label을 `candidate.LineId` 만으로 변경.

- [ ] **Step 6: Commit**

```bash
git add "Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Data/Definitions/Editor/StoryPreviewWindow.Inspector.cs"
git commit -m "feat: Import Previous Stage multi-predecessor selection menu"
```

---

## Unity 확인 체크리스트

작업 완료 후 Unity Editor에서 직접 확인:

### Easing 기본값
- [ ] 새 numeric key 생성 시 inspector에 easing이 `None`으로 표시되는지
- [ ] 기존 asset의 키는 여전히 `EaseInOut`으로 유지되는지 (기존 데이터 보존)

### Single key + pre-first segment
- [ ] key가 하나이고 time > 0이면 Timeline row에서 line start → key segment 선이 보이는지
- [ ] 그 segment를 클릭하면 Inspector에 "Line Start → X.XXs" 레이블이 뜨는지
- [ ] Inspector에서 easing 변경이 가능한지
- [ ] 첫 key가 time 0이면 pre-first segment가 표시되지 않는지

### None easing 동작 (playhead scrub)
- [ ] single key easing = None → key time 이전: playhead를 드래그하면 base state(line start state) 유지되는지
- [ ] single key easing = None → key time에서: actor/camera/background가 key 값으로 즉시 snap하는지
- [ ] single key easing = EaseInOut → key time 이전: line start state에서 key까지 tween하는지

### 두 key 사이 arriving easing
- [ ] 두 key A, B 사이에서 B.easing이 적용되는지 (A→B segment에서 B의 easing이 색상으로 표시)
- [ ] B.easing = None이면 A time까지 A 값 유지, B time에 snap되는지
- [ ] B.easing = Linear이면 A→B 선형 보간이 되는지

### Numeric vs Discrete
- [ ] Position / Scale / Camera Position / Camera Zoom / Background Position / Background Scale 에 segment bar 있는지
- [ ] Expression / Background Cut에 segment bar 없는지

### Preview/Runtime 일치
- [ ] Preview scrub과 "Preview Line Motion" 재생이 같은 결과를 내는지
- [ ] Play Mode에서 runtime 재생도 sampler 결과와 일치하는지

### Import Previous Stage
- [ ] 단일 predecessor → 즉시 import 진행 (현재와 동일)
- [ ] import 후 현재 line에 actor/bg/camera keyframe이 복사되지 않고 비어 있는지
- [ ] import 후 현재 line의 base state가 이전 line의 마지막 sampled state와 일치하는지
- [ ] 여러 predecessor가 있으면 context menu가 뜨는지
- [ ] menu에서 선택한 predecessor의 final state를 가져오는지

---

## 다음 Phase 전 남은 리스크

1. **기존 EaseInOut 키의 시각적 변화**: arriving 모델 전환으로 기존 다중 키 데이터의 애니메이션이 의도와 달라 보일 수 있음. 기존 모든 StoryStageLayoutModuleSO asset을 재검토해야 함.

2. **`StoryLineSO` 프로퍼티 접근**: `OnImportPreviousStageClicked`의 label에서 Speaker/DialogueText 접근 시 private field라면 컴파일 에러. Task 4 Step 5에서 반드시 확인.

3. **Camera/Background base state 계산**: `SampleCameraTrackAtTime`에서 base값으로 `sample.stageLocalPosition`(defaultState 복사본)을 넘기므로, defaultState가 올바르게 초기화되어 있어야 함. 기존 코드에서 `track?.defaultState?.ShallowClone() ?? new StoryCameraStateData()`로 초기화 → 안전.

4. **Pre-first segment와 단일 키 이전 동작 차이**: 기존에는 `TryFindSegment` 가 `from=to=keys[0]`을 반환해 첫 키 값을 t=0부터 표시했음. 이제 base state를 표시하므로 기존 authoring된 line start state와 달라 보일 수 있음. 의도된 변경.

5. **`ShouldStartTimelineGroupDragFromSegment(e, from, to)`**: pre-first segment에서 `from = null`로 호출됨. 이 함수가 `from`을 null-check하는지 확인 필요. 안전하게 `from != null &&` guard 추가 (Step 2에 포함됨).
