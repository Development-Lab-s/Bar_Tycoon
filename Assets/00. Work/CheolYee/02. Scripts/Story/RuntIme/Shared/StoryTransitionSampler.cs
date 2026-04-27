using System.Collections.Generic;
using UnityEngine;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;

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
                float p = ResolveEnterProgress(to.enterMotion, to.enterDuration, elapsed);
                StoryActorStateData sample = to.ShallowClone();
                sample.EnsureActorInstanceKey(actorKey);
                sample.normalizedPosition = Vector2.LerpUnclamped(EnterStartPosition(to), to.normalizedPosition, p);
                sample.scale              = Vector2.LerpUnclamped(EnterStartScale(to), to.scale, p);
                sample.visible            = true;
                sample.focusVisualAlpha   = ResolveFocusAlpha(to.focused);
                return sample;
            }

            if (fromVisible && !toVisible)
            {
                float p = ResolveEnterProgress(from.exitMotion, from.exitDuration, elapsed);
                StoryActorStateData sample = from.ShallowClone();
                sample.EnsureActorInstanceKey(actorKey);
                sample.normalizedPosition = Vector2.LerpUnclamped(from.normalizedPosition, ExitEndPosition(from), p);
                sample.scale              = Vector2.LerpUnclamped(from.scale, ExitEndScale(from), p);
                sample.visible            = p < 1f;
                sample.focusVisualAlpha   = ResolveFocusAlpha(from.focused);
                return sample.visible ? sample : null;
            }

            // Both visible — move transition
            float moveP = ResolveMoveProgress(to.moveMotion, to.moveDuration, elapsed);
            StoryActorStateData moved = to.ShallowClone();
            moved.EnsureActorInstanceKey(actorKey);
            moved.normalizedPosition = Vector2.LerpUnclamped(from.normalizedPosition, to.normalizedPosition, moveP);
            moved.scale              = Vector2.LerpUnclamped(from.scale, to.scale, moveP);
            moved.scaleX             = Mathf.LerpUnclamped(from.scaleX, to.scaleX, moveP);
            moved.visible            = true;
            moved.focusVisualAlpha   = SampleFocusAlpha(from, to, elapsed);
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

            if (TrySampleVector2(track, StoryActorKeyframeProperty.Position, t, k => k.normalizedPosition, out Vector2 position))
                sample.normalizedPosition = position;

            if (TrySampleVector2(track, StoryActorKeyframeProperty.Scale, t, k => k.scale, out Vector2 scale))
                sample.scale = scale;

            if (TrySampleFloat(track, StoryActorKeyframeProperty.Scale, t, k => k.scaleX, out float scaleX))
                sample.scaleX = scaleX;

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
            out Vector2 value)
        {
            value = default;
            if (!TryFindSegment(track, property, time, out var from, out var to, out float local))
                return false;

            if (to == null || from == to)
            {
                value = selector(from);
                return true;
            }

            local = ResolveMoveProgress(ResolveOutgoingEasing(track, from), 1f, local);
            value = Vector2.LerpUnclamped(selector(from), selector(to), local);
            return true;
        }

        private static bool TrySampleFloat(
            StoryActorTrackData track,
            StoryActorKeyframeProperty property,
            float time,
            System.Func<StoryActorKeyframeData, float> selector,
            out float value)
        {
            value = default;
            if (!TryFindSegment(track, property, time, out var from, out var to, out float local))
                return false;

            if (to == null || from == to)
            {
                value = selector(from);
                return true;
            }

            local = ResolveMoveProgress(ResolveOutgoingEasing(track, from), 1f, local);
            value = Mathf.LerpUnclamped(selector(from), selector(to), local);
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

            if (time <= GetKeyTime(keys[0]))
            {
                from = keys[0];
                to = keys[0];
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

        private static StoryStageMoveMotionType ResolveOutgoingEasing(StoryActorTrackData track, StoryActorKeyframeData from)
        {
            float time = GetKeyTime(from);
            foreach (StoryActorKeyframeData keyframe in track.keyframes)
            {
                if (keyframe != null
                    && keyframe.property == StoryActorKeyframeProperty.Easing
                    && Mathf.Approximately(GetKeyTime(keyframe), time))
                    return keyframe.easing;
            }

            return from.easing;
        }

        // ── Background sampling ───────────────────────────────────────────────

        public static StoryBackgroundStateData SampleBackground(
            StoryBackgroundStateData from,
            StoryBackgroundStateData to,
            float elapsed)
        {
            bool fromVisible = from != null && from.HasBackground && from.visible;
            bool toVisible   = to   != null && to.HasBackground   && to.visible;

            if (!fromVisible && !toVisible)
                return null;

            if (!fromVisible && toVisible)
            {
                float p = ResolveEnterProgress(to.transitionMotion, to.transitionDuration, elapsed);
                StoryBackgroundStateData sample = to.ShallowClone();
                sample.normalizedOffset = Vector2.LerpUnclamped(BgEnterOffset(to), to.normalizedOffset, p);
                sample.scale            = Vector2.LerpUnclamped(BgEnterScale(to), to.scale, p);
                sample.opacity          = Mathf.Lerp(0f, to.opacity, p);
                return sample;
            }

            if (fromVisible && !toVisible)
            {
                float p = ResolveEnterProgress(from.exitMotion, from.exitDuration, elapsed);
                StoryBackgroundStateData sample = from.ShallowClone();
                sample.normalizedOffset = Vector2.LerpUnclamped(from.normalizedOffset, BgExitOffset(from), p);
                sample.opacity          = Mathf.Lerp(from.opacity, 0f, p);
                sample.visible          = p < 1f;
                return sample.visible ? sample : null;
            }

            // Both visible — blend
            float progress = ResolveEnterProgress(to.transitionMotion, to.transitionDuration, elapsed);
            StoryBackgroundStateData blended = to.ShallowClone();
            blended.normalizedOffset = Vector2.LerpUnclamped(from.normalizedOffset, to.normalizedOffset, progress);
            blended.scale            = Vector2.LerpUnclamped(from.scale, to.scale, progress);
            blended.opacity          = Mathf.Lerp(from.opacity, to.opacity, progress);
            blended.tint             = Color.Lerp(from.tint, to.tint, progress);
            return blended;
        }

        // ── Duration helpers ─────────────────────────────────────────────────

        public static float ActorTransitionDuration(StoryActorStateData from, StoryActorStateData to)
        {
            bool fv = from is { visible: true };
            bool tv = to   is { visible: true };

            if (!fv && tv) return to.enterMotion  == StoryEnterMotionType.Instant ? 0.05f : Mathf.Max(0.05f, to.enterDuration);
            if (fv && !tv) return from.exitMotion  == StoryEnterMotionType.Instant ? 0.05f : Mathf.Max(0.05f, from.exitDuration);
            if (fv)        return to.moveMotion    == StoryStageMoveMotionType.Instant ? 0.05f : Mathf.Max(0.05f, to.moveDuration);
            return 0.05f;
        }

        public static float BackgroundTransitionDuration(StoryBackgroundStateData from, StoryBackgroundStateData to)
        {
            bool fv = from != null && from.HasBackground && from.visible;
            bool tv = to   != null && to.HasBackground   && to.visible;

            if (!fv && tv) return to.transitionMotion == StoryEnterMotionType.Instant ? 0.05f : Mathf.Max(0.05f, to.transitionDuration);
            if (fv && !tv) return from.exitMotion     == StoryEnterMotionType.Instant ? 0.05f : Mathf.Max(0.05f, from.exitDuration);
            if (tv)        return to.transitionMotion == StoryEnterMotionType.Instant ? 0.05f : Mathf.Max(0.05f, to.transitionDuration);
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

            float t = Mathf.Clamp01(elapsed / duration);
            return motion switch
            {
                StoryStageMoveMotionType.EaseIn        => t * t * t,
                StoryStageMoveMotionType.EaseOut       => 1f - Mathf.Pow(1f - t, 3f),
                StoryStageMoveMotionType.EaseInOut     => Mathf.SmoothStep(0f, 1f, t),
                StoryStageMoveMotionType.SmoothStep    => Mathf.SmoothStep(0f, 1f, t),
                StoryStageMoveMotionType.SmootherStep  => t * t * t * (t * (6f * t - 15f) + 10f),
                _                                      => t
            };
        }

        // ── Position / scale helpers ─────────────────────────────────────────

        public static Vector2 EnterStartPosition(StoryActorStateData target) =>
            target.enterMotion switch
            {
                StoryEnterMotionType.SlideFromLeft  => target.normalizedPosition + new Vector2(-0.45f, 0f),
                StoryEnterMotionType.SlideFromRight => target.normalizedPosition + new Vector2( 0.45f, 0f),
                _                                   => target.normalizedPosition
            };

        public static Vector2 ExitEndPosition(StoryActorStateData source) =>
            source.exitMotion switch
            {
                StoryEnterMotionType.SlideFromLeft  => source.normalizedPosition + new Vector2(-0.45f, 0f),
                StoryEnterMotionType.SlideFromRight => source.normalizedPosition + new Vector2( 0.45f, 0f),
                _                                   => source.normalizedPosition
            };

        public static Vector2 EnterStartScale(StoryActorStateData target) =>
            target.enterMotion == StoryEnterMotionType.ZoomIn ? Vector2.zero : target.scale;

        public static Vector2 ExitEndScale(StoryActorStateData source) =>
            source.exitMotion == StoryEnterMotionType.ZoomIn ? Vector2.zero : source.scale;

        public static Vector2 BgEnterOffset(StoryBackgroundStateData target) =>
            target.transitionMotion switch
            {
                StoryEnterMotionType.SlideFromLeft  => target.normalizedOffset + new Vector2(-1f, 0f),
                StoryEnterMotionType.SlideFromRight => target.normalizedOffset + new Vector2( 1f, 0f),
                _                                   => target.normalizedOffset
            };

        public static Vector2 BgExitOffset(StoryBackgroundStateData source) =>
            source.exitMotion switch
            {
                StoryEnterMotionType.SlideFromLeft  => source.normalizedOffset + new Vector2(-1f, 0f),
                StoryEnterMotionType.SlideFromRight => source.normalizedOffset + new Vector2( 1f, 0f),
                _                                   => source.normalizedOffset
            };

        public static Vector2 BgEnterScale(StoryBackgroundStateData target) =>
            target.transitionMotion == StoryEnterMotionType.ZoomIn ? Vector2.zero : target.scale;
    }
}
