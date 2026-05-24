using System;
using System.Collections.Generic;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.CameraSystems;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.Util;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    /// <summary>
    /// Runtime stage coordinator.
    ///
    /// This class intentionally does not create actors/backgrounds, sample transition data,
    /// or touch Cinemachine directly. It coordinates small runtime components.
    /// </summary>
    public sealed class BasicStoryCharacterStageDirector : MonoBehaviour, ICharacterStageDirector, IStoryStageDirector
    {
        private const float ImmediateTransitionThreshold = 0.05f;

        [Header("Runtime Components")]
        [SerializeField] private StoryActorStageRuntime actorRuntime;
        [SerializeField] private StoryBackgroundStageRuntime backgroundRuntime;
        [SerializeField] private StoryStageCameraRuntime cameraRuntime;

        private readonly StoryStageTransitionRuntime _transitionRuntime = new();

        public UniTask EnsureSpeakerVisibleAsync(StoryLineSO line, CancellationToken ct)
        {
            return ResolveActorRuntime().EnsureSpeakerVisibleAsync(line, ct);
        }

        public void ApplySpeakerFocus(StoryLineSO line)
        {
            ResolveActorRuntime().ApplySpeakerFocus(line);
        }

        public void ApplyBackgroundOnly(StoryStageLayoutModuleSO layout)
        {
            ResolveActorRuntime().ClearAll();
            StoryBackgroundStateData state = layout != null && layout.HasBackground
                ? layout.Background.ShallowClone()
                : null;
            ResolveBackgroundRuntime().ApplyState(state);
        }

        public UniTask PrewarmLinePresentationAsync(StoryLineSO line, CancellationToken ct)
        {
            if (line == null)
                return UniTask.CompletedTask;

            StoryStageLayoutModuleSO layout = FindStageLayout(line);
            StoryBackgroundStateData state = layout != null && layout.HasBackground
                ? layout.Background.ShallowClone()
                : null;
            ResolveBackgroundRuntime().ApplyState(state);

            Dictionary<string, StoryActorStateData> sampledActors = null;
            if (layout != null)
            {
                Dictionary<string, StoryActorStateData> targetMap = _transitionRuntime.BuildTargetMap(layout.Actors);
                Dictionary<string, StoryActorTrackData> trackMap = _transitionRuntime.BuildTrackMap(layout.ActorTracks);
                sampledActors = _transitionRuntime.SampleActors(targetMap, targetMap, trackMap, 0f);
                ResolveActorRuntime().PrewarmHiddenActors(sampledActors);

                bool useCameraTrack = _transitionRuntime.HasAuthoredCameraTrack(layout.CameraTrack);
                ApplyCamera(layout, useCameraTrack, 0f, 0f, sampledActors);
            }

            if (line.Speaker != null)
                ResolveActorRuntime().PrewarmHiddenSpeaker(line.Speaker);

            return UniTask.CompletedTask;
        }

        public void ApplyStageLayoutImmediate(StoryStageLayoutModuleSO layout)
        {
            if (layout == null)
                return;

            Dictionary<string, StoryActorStateData> targetMap = _transitionRuntime.BuildTargetMap(layout.Actors);
            Dictionary<string, StoryActorTrackData> trackMap = _transitionRuntime.BuildTrackMap(layout.ActorTracks);
            Dictionary<string, StoryActorStateData> sampledActors = _transitionRuntime.SampleActors(
                targetMap,
                targetMap,
                trackMap,
                0f);
            ApplyActorSamples(sampledActors);

            StoryBackgroundStateData fromBackground = ResolveBackgroundRuntime().BuildCurrentStateClone();
            ApplyBackgroundState(_transitionRuntime.SampleBackground(
                fromBackground,
                layout.Background,
                layout.BackgroundTrack,
                0f));

            bool useCameraTrack = _transitionRuntime.HasAuthoredCameraTrack(layout.CameraTrack);
            ApplyCamera(layout, useCameraTrack, 0f, 0f, sampledActors);
        }

        public Vector3 GetCurrentCameraCenter()
        {
            StoryStageCameraRuntime runtime = ResolveCameraRuntime();
            return runtime != null ? runtime.GetCurrentCameraCenter() : Vector3.zero;
        }

        public void ClearAll()
        {
            ResolveActorRuntime().ClearAll();
            ResolveBackgroundRuntime().ClearAll();
        }

        // ── IStoryStageDirector ───────────────────────────────────────────────

        public async UniTask ApplyStageLayoutAsync(StoryStageLayoutModuleSO layout, CancellationToken ct)
        {
            if (layout == null)
            {
                await ApplyStageStateAsync(null, ct);
                return;
            }

            Dictionary<string, StoryActorStateData> targetMap = _transitionRuntime.BuildTargetMap(layout.Actors);
            Dictionary<string, StoryActorTrackData> trackMap = _transitionRuntime.BuildTrackMap(layout.ActorTracks);
            Dictionary<string, StoryActorStateData> fromMap = ResolveActorRuntime().BuildCurrentActorStateMap();
            StoryBackgroundStateData fromBackground = ResolveBackgroundRuntime().BuildCurrentStateClone();
            bool useCameraTrack = _transitionRuntime.HasAuthoredCameraTrack(layout.CameraTrack);

            float duration = _transitionRuntime.CalculateDuration(
                fromMap,
                targetMap,
                trackMap,
                fromBackground,
                layout.Background,
                layout.BackgroundTrack,
                layout.CameraTrack,
                useCameraTrack);

            if (duration <= ImmediateTransitionThreshold)
            {
                ApplyStageSamples(
                    fromMap,
                    targetMap,
                    trackMap,
                    fromBackground,
                    layout,
                    useCameraTrack,
                    previousCameraTime: 0f,
                    currentTime: duration,
                    applyCamera: true);

                return;
            }

            await PlayStageTransitionAsync(
                layout,
                fromMap,
                targetMap,
                trackMap,
                fromBackground,
                useCameraTrack,
                duration,
                ct);
        }

        public UniTask ApplyStageStateAsync(IReadOnlyList<StoryActorStateData> targetActors, CancellationToken ct)
        {
            ApplyActorSamples(_transitionRuntime.BuildTargetMap(targetActors));
            return UniTask.CompletedTask;
        }

        private async UniTask PlayStageTransitionAsync(
            StoryStageLayoutModuleSO layout,
            Dictionary<string, StoryActorStateData> fromMap,
            Dictionary<string, StoryActorStateData> targetMap,
            Dictionary<string, StoryActorTrackData> trackMap,
            StoryBackgroundStateData fromBackground,
            bool useCameraTrack,
            float duration,
            CancellationToken ct)
        {
            float previousCameraTime = 0f;

            ApplyStageSamples(
                fromMap,
                targetMap,
                trackMap,
                fromBackground,
                layout,
                useCameraTrack,
                previousCameraTime: 0f,
                currentTime: 0f,
                applyCamera: true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                ApplyStageSamples(
                    fromMap,
                    targetMap,
                    trackMap,
                    fromBackground,
                    layout,
                    useCameraTrack,
                    previousCameraTime,
                    elapsed,
                    applyCamera: useCameraTrack);

                if (useCameraTrack)
                    previousCameraTime = elapsed;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.deltaTime;
            }

            ApplyStageSamples(
                fromMap,
                targetMap,
                trackMap,
                fromBackground,
                layout,
                useCameraTrack,
                previousCameraTime,
                duration,
                applyCamera: true);
        }

        private void ApplyStageSamples(
            Dictionary<string, StoryActorStateData> fromMap,
            Dictionary<string, StoryActorStateData> targetMap,
            Dictionary<string, StoryActorTrackData> trackMap,
            StoryBackgroundStateData fromBackground,
            StoryStageLayoutModuleSO layout,
            bool useCameraTrack,
            float previousCameraTime,
            float currentTime,
            bool applyCamera)
        {
            Dictionary<string, StoryActorStateData> actorSamples =
                _transitionRuntime.SampleActors(fromMap, targetMap, trackMap, currentTime);
            ApplyActorSamples(actorSamples);
            ApplyBackgroundState(_transitionRuntime.SampleBackground(
                fromBackground,
                layout.Background,
                layout.BackgroundTrack,
                currentTime));
            if (applyCamera)
                ApplyCamera(layout, useCameraTrack, previousCameraTime, currentTime, actorSamples);
        }

        private void ApplyActorSamples(Dictionary<string, StoryActorStateData> targetMap)
        {
            ResolveActorRuntime().ApplySamples(targetMap);
        }

        private void ApplyBackgroundState(StoryBackgroundStateData state)
        {
            ResolveBackgroundRuntime().ApplyState(state);
        }

        private void ApplyCamera(
            StoryStageLayoutModuleSO layout,
            bool useCameraTrack,
            float previousTime,
            float currentTime,
            Dictionary<string, StoryActorStateData> actorSamples = null)
        {
            Func<string, Vector2?> resolver = null;
            if (actorSamples != null)
                resolver = key => actorSamples.TryGetValue(key, out StoryActorStateData s) ? s?.stageLocalPosition : null;
            ResolveCameraRuntime().ApplyCamera(layout, useCameraTrack, previousTime, currentTime, _transitionRuntime, resolver);
        }

        private StoryActorStageRuntime ResolveActorRuntime()
        {
            return StoryRuntimeComponentResolver.GetOnSelfOrAdd(this, ref actorRuntime);
        }

        private StoryBackgroundStageRuntime ResolveBackgroundRuntime()
        {
            return StoryRuntimeComponentResolver.GetOnSelfOrAdd(this, ref backgroundRuntime);
        }

        private StoryStageCameraRuntime ResolveCameraRuntime()
        {
            return StoryRuntimeComponentResolver.GetOnSelfOrAdd(this, ref cameraRuntime);
        }

        private static StoryStageLayoutModuleSO FindStageLayout(StoryLineSO line)
        {
            if (line?.Modules == null)
                return null;

            foreach (StoryModuleSO module in line.Modules)
            {
                if (module is StoryStageLayoutModuleSO layout)
                    return layout;
            }

            return null;
        }
    }
}
