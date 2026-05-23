using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using UnityEngine;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared
{
    /// <summary>
    /// Pure-static math for actor and background line-transition sampling.
    /// Used by both StoryPreviewWindow (editor) and StoryStageLayoutModuleExecutor (runtime).
    /// No allocation-heavy operations; callers supply from/to state, get a sampled clone back.
    /// </summary>
    public static class StoryTransitionSampler
    {
        public const float FocusedActorFocusAlpha = 1f;
        public const float DimmedActorFocusAlpha = 0.65f;
        private const float FocusFadeDuration = 0.18f;

        // ── Actor sampling ────────────────────────────────────────────────────

        /// <summary>
        /// Returns a sampled StoryActorStateData at <paramref name="elapsed"/> seconds into
        /// the transition from <paramref name="from"/> to <paramref name="to"/>.
        /// Returns null when both states are invisible (actor not on stage).
        /// </summary>
        public static StoryActorStateData SampleActor(
            string actorKey,
            StoryActorStateData from,
            StoryActorStateData to,
            float elapsed)
        {
            bool fromVisible = from is { visible: true };
            bool toVisible   = to   is { visible: true };

            if (!fromVisible && !toVisible)
                return null;

            if (!fromVisible && toVisible)
            {
                StoryActorStateData sample = to.ShallowClone();
                sample.EnsureActorInstanceKey(actorKey);
                sample.visible          = true;
                sample.focusVisualAlpha = ResolveFocusAlpha(to.focused);
                return sample;
            }

            if (fromVisible && !toVisible)
                return null;

            // Both visible — instant snap to target state
            StoryActorStateData moved = to.ShallowClone();
            moved.EnsureActorInstanceKey(actorKey);
            moved.visible          = true;
            moved.focusVisualAlpha = SampleFocusAlpha(from, to, elapsed);
            return moved;
        }

        public static StoryActorStateData SampleActorTrack(
            StoryActorStateData baseState,
            StoryActorTrackData track,
            float normalizedTime)
        {
            return SampleActorTrackAtTime(baseState, track, normalizedTime * GetActorTrackDuration(track));
        }

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

        public static float ResolveFocusAlpha(bool focused) =>
            focused ? FocusedActorFocusAlpha : DimmedActorFocusAlpha;

        public static float ResolveFocusBlend(float focusAlpha) =>
            Mathf.InverseLerp(DimmedActorFocusAlpha, FocusedActorFocusAlpha, Mathf.Clamp01(focusAlpha));

        public static float SampleFocusAlpha(StoryActorStateData from, StoryActorStateData to, float elapsed)
        {
            bool fromFocused = from?.focused ?? to?.focused ?? true;
            bool toFocused = to?.focused ?? fromFocused;
            float fromAlpha = ResolveFocusAlpha(fromFocused);
            float toAlpha = ResolveFocusAlpha(toFocused);
            if (Mathf.Approximately(fromAlpha, toAlpha))
                return toAlpha;

            float p = ResolveMoveProgress(StoryStageMoveMotionType.EaseInOut, FocusFadeDuration, elapsed);
            return Mathf.Lerp(fromAlpha, toAlpha, p);
        }

        public static float GetActorTrackDuration(StoryActorTrackData track)
        {
            if (track == null || track.keyframes == null || track.keyframes.Count == 0)
                return 0f;

            float duration = 0f;
            foreach (StoryActorKeyframeData keyframe in track.keyframes)
            {
                if (keyframe == null)
                    continue;

                duration = Mathf.Max(duration, GetKeyTime(keyframe));
            }

            return duration;
        }

        public static float GetKeyTime(StoryActorKeyframeData keyframe)
        {
            if (keyframe == null)
                return 0f;

            return keyframe.timeSeconds > 0f
                ? keyframe.timeSeconds
                : keyframe.normalizedTime;
        }

        private static bool TrySampleVector2(
            StoryActorTrackData track,
            StoryActorKeyframeProperty property,
            float time,
            System.Func<StoryActorKeyframeData, Vector2> selector,
            Vector2 baseValue,
            out Vector2 value)
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
            Vector2 fromValue = from != null ? selector(from) : baseValue;
            value = Vector2.LerpUnclamped(fromValue, selector(to), progress);
            return true;
        }

        private static bool TrySampleFloat(
            StoryActorTrackData track,
            StoryActorKeyframeProperty property,
            float time,
            System.Func<StoryActorKeyframeData, float> selector,
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

        private static bool TrySampleExpression(
            StoryActorTrackData track,
            float time,
            out StoryExpressionType expression)
        {
            expression = default;
            if (track == null || track.keyframes == null)
                return false;

            StoryActorKeyframeData latest = null;
            foreach (StoryActorKeyframeData keyframe in track.keyframes)
            {
                if (keyframe == null || keyframe.property != StoryActorKeyframeProperty.Expression)
                    continue;

                float keyTime = GetKeyTime(keyframe);
                if (keyTime <= time && (latest == null || keyTime >= GetKeyTime(latest)))
                    latest = keyframe;
            }

            if (latest == null)
                return false;

            expression = latest.expression;
            return true;
        }

        // ── Background sampling ───────────────────────────────────────────────

        /// <summary>
        /// Returns the target background state as an instant cut.
        /// Auto-transition (fade-in, slide) has been removed. All changes are immediate.
        /// </summary>
        public static StoryBackgroundStateData SampleBackground(
            StoryBackgroundStateData from,
            StoryBackgroundStateData to,
            float elapsed)
        {
            bool toVisible = to != null && to.HasBackground && to.visible;
            return toVisible ? to.ShallowClone() : null;
        }

        public static StoryBackgroundStateData SampleBackgroundTrackAtTime(
            StoryBackgroundStateData baseState,
            StoryBackgroundTrackData track,
            float timeSeconds)
        {
            if (track == null || track.keyframes == null || track.keyframes.Count == 0)
                return baseState != null ? baseState.ShallowClone() : null;

            StoryBackgroundStateData sample = baseState != null
                ? baseState.ShallowClone()
                : new StoryBackgroundStateData { visible = false };
            float t = Mathf.Max(0f, timeSeconds);

            if (TrySampleBackgroundCut(track, t, out StoryActorKeyframeData cut))
            {
                sample.background = cut.background;
                sample.backgroundKey = !string.IsNullOrWhiteSpace(cut.backgroundKey)
                    ? cut.backgroundKey
                    : ResolveBackgroundKey(cut.background);
                sample.visible = cut.background != null || !string.IsNullOrWhiteSpace(sample.backgroundKey);
            }

            if (TrySampleBackgroundVector2(track, StoryActorKeyframeProperty.BackgroundPosition, t,
                    k => k.stageLocalPosition, sample.stageLocalPosition, out Vector2 pos))
                sample.stageLocalPosition = pos;

            if (TrySampleBackgroundFloat(track, StoryActorKeyframeProperty.BackgroundScale, t,
                    k => k.scale.y, sample.scaleMultiplier, out float bgScale))
                sample.scaleMultiplier = Mathf.Max(0.001f, bgScale);

            return sample;
        }

        public static float GetBackgroundTrackDuration(StoryBackgroundTrackData track)
        {
            if (track == null || track.keyframes == null || track.keyframes.Count == 0)
                return 0f;

            float duration = 0f;
            foreach (StoryActorKeyframeData keyframe in track.keyframes)
            {
                if (keyframe == null)
                    continue;

                duration = Mathf.Max(duration, GetKeyTime(keyframe));
            }

            return duration;
        }

        public static StoryCameraStateData SampleCameraTrackAtTime(
            StoryCameraTrackData track,
            string fallbackTargetActorKey,
            float timeSeconds)
        {
            StoryCameraStateData sample = track?.defaultState != null
                ? track.defaultState.ShallowClone()
                : new StoryCameraStateData();

            if (string.IsNullOrWhiteSpace(sample.targetActorInstanceKey))
                sample.targetActorInstanceKey = fallbackTargetActorKey ?? string.Empty;

            if (track == null || track.keyframes == null || track.keyframes.Count == 0)
                return sample;

            float t = Mathf.Max(0f, timeSeconds);
            if (TrySampleCameraTarget(track, t, out StoryActorKeyframeData targetKey))
            {
                sample.targetActorInstanceKey = targetKey.cameraTargetActorKey ?? string.Empty;
                sample.followMode = targetKey.cameraFollowMode;
                sample.moveMode = targetKey.cameraMoveMode;
                sample.snapshotNormalizedPosition = targetKey.cameraSnapshotNormalizedPosition;
            }

            if (TrySampleCameraVector2(track, StoryActorKeyframeProperty.CameraOffset, t,
                    k => k.cameraStageLocalPosition, sample.stageLocalPosition, out Vector2 camPos))
                sample.stageLocalPosition = camPos;

            if (TrySampleCameraFloat(track, StoryActorKeyframeProperty.CameraZoom, t,
                    k => k.cameraZoom, sample.zoom, out float zoom))
                sample.zoom = Mathf.Max(0.01f, zoom);

            if (string.IsNullOrWhiteSpace(sample.targetActorInstanceKey))
                sample.targetActorInstanceKey = fallbackTargetActorKey ?? string.Empty;

            return sample;
        }

        public static float GetCameraTrackDuration(StoryCameraTrackData track)
        {
            if (track == null || track.keyframes == null || track.keyframes.Count == 0)
                return 0f;

            float duration = 0f;
            foreach (StoryActorKeyframeData keyframe in track.keyframes)
            {
                if (keyframe == null)
                    continue;

                duration = Mathf.Max(duration, GetKeyTime(keyframe));
            }

            return duration;
        }

        public static void CollectCameraShakeKeysBetween(
            StoryCameraTrackData track,
            float previousTime,
            float currentTime,
            List<StoryActorKeyframeData> results)
        {
            results?.Clear();
        }

        private static bool TrySampleBackgroundCut(
            StoryBackgroundTrackData track,
            float time,
            out StoryActorKeyframeData cut)
        {
            cut = null;
            foreach (StoryActorKeyframeData keyframe in track.keyframes)
            {
                if (keyframe == null || keyframe.property != StoryActorKeyframeProperty.BackgroundCut)
                    continue;

                float keyTime = GetKeyTime(keyframe);
                if (keyTime <= time && (cut == null || keyTime >= GetKeyTime(cut)))
                    cut = keyframe;
            }

            return cut != null;
        }

        private static bool TrySampleCameraTarget(
            StoryCameraTrackData track,
            float time,
            out StoryActorKeyframeData target)
        {
            target = null;
            if (track == null || track.keyframes == null)
                return false;

            foreach (StoryActorKeyframeData keyframe in track.keyframes)
            {
                if (keyframe == null || keyframe.property != StoryActorKeyframeProperty.CameraTarget)
                    continue;

                float keyTime = GetKeyTime(keyframe);
                if (keyTime <= time && (target == null || keyTime >= GetKeyTime(target)))
                    target = keyframe;
            }

            return target != null;
        }

        public static bool TryGetCameraTargetKeys(
            StoryCameraTrackData track,
            float time,
            out StoryActorKeyframeData previous,
            out StoryActorKeyframeData current)
        {
            previous = null;
            current = null;
            if (track == null || track.keyframes == null)
                return false;

            var keys = new List<StoryActorKeyframeData>();
            foreach (StoryActorKeyframeData keyframe in track.keyframes)
            {
                if (keyframe != null && keyframe.property == StoryActorKeyframeProperty.CameraTarget)
                    keys.Add(keyframe);
            }

            keys.Sort((a, b) => GetKeyTime(a).CompareTo(GetKeyTime(b)));
            if (keys.Count == 0)
                return false;

            for (int i = 0; i < keys.Count; i++)
            {
                float keyTime = GetKeyTime(keys[i]);
                if (keyTime <= time)
                {
                    current = keys[i];
                    if (i > 0)
                        previous = keys[i - 1];
                }
                else
                {
                    break;
                }
            }

            return current != null;
        }

        private static bool TrySampleCameraVector2(
            StoryCameraTrackData track,
            StoryActorKeyframeProperty property,
            float time,
            System.Func<StoryActorKeyframeData, Vector2> selector,
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
            System.Func<StoryActorKeyframeData, float> selector,
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

        private static bool TrySampleBackgroundVector2(
            StoryBackgroundTrackData track,
            StoryActorKeyframeProperty property,
            float time,
            System.Func<StoryActorKeyframeData, Vector2> selector,
            Vector2 baseValue,
            out Vector2 value)
        {
            value = default;
            if (!TryFindBackgroundSegment(track, property, time, out var from, out var to, out float local))
                return false;

            if (to == null) return false;
            if (from == to) { value = selector(from); return true; }

            float progress = ResolveMoveProgress(to.easing, 1f, local);
            Vector2 fromValue = from != null ? selector(from) : baseValue;
            value = Vector2.LerpUnclamped(fromValue, selector(to), progress);
            return true;
        }

        private static bool TrySampleBackgroundFloat(
            StoryBackgroundTrackData track,
            StoryActorKeyframeProperty property,
            float time,
            System.Func<StoryActorKeyframeData, float> selector,
            float baseValue,
            out float value)
        {
            value = default;
            if (!TryFindBackgroundSegment(track, property, time, out var from, out var to, out float local))
                return false;

            if (to == null) return false;
            if (from == to) { value = selector(from); return true; }

            float progress = ResolveMoveProgress(to.easing, 1f, local);
            float fromValue = from != null ? selector(from) : baseValue;
            value = Mathf.LerpUnclamped(fromValue, selector(to), progress);
            return true;
        }

        private static bool TryFindBackgroundSegment(
            StoryBackgroundTrackData track,
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

        private static string ResolveBackgroundKey(BackgroundDefinitionSO background)
        {
            if (background == null)
                return string.Empty;

            return !string.IsNullOrWhiteSpace(background.BackgroundId)
                ? background.BackgroundId
                : background.name;
        }

        // ── Duration helpers ─────────────────────────────────────────────────

        public static float ActorTransitionDuration(StoryActorStateData from, StoryActorStateData to) => 0.05f;

        public static float BackgroundTransitionDuration(StoryBackgroundStateData from, StoryBackgroundStateData to)
        {
            return 0.05f;
        }

        // ── Progress curves ───────────────────────────────────────────────────

        /// <summary>
        /// Maps elapsed time to a [0,1] progress value using the given enter motion curve.
        /// Returns 1 immediately for Instant or zero/negative duration.
        /// </summary>
        public static float ResolveEnterProgress(StoryEnterMotionType motion, float duration, float elapsed)
        {
            if (motion == StoryEnterMotionType.Instant || duration <= 0f)
                return 1f;

            float t = Mathf.Clamp01(elapsed / duration);
            return motion switch
            {
                StoryEnterMotionType.FadeIn          => Mathf.SmoothStep(0f, 1f, t),
                StoryEnterMotionType.ZoomIn          => t * t,                      // ease-in quad (pop-in feel)
                StoryEnterMotionType.SlideFromLeft   => t * t * (3f - 2f * t),     // smoothstep slide
                StoryEnterMotionType.SlideFromRight  => t * t * (3f - 2f * t),
                _                                    => Mathf.SmoothStep(0f, 1f, t)
            };
        }

        public static float ResolveMoveProgress(StoryStageMoveMotionType motion, float duration, float elapsed)
        {
            if (motion == StoryStageMoveMotionType.Instant || duration <= 0f)
                return 1f;

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

    }
}
