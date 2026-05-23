using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    /// <summary>
    /// Runtime-only transition/sampling cache for stage layout playback.
    ///
    /// This class owns the reusable dictionaries/lists needed to sample a StoryStageLayoutModuleSO.
    /// It does not create GameObjects and does not touch camera/actor/background components directly.
    /// </summary>
    public sealed class StoryStageTransitionRuntime
    {
        private readonly Dictionary<string, StoryActorStateData> _targetActorMap = new();
        private readonly Dictionary<string, StoryActorTrackData> _actorTrackMap = new();
        private readonly Dictionary<string, StoryActorStateData> _actorSampleMap = new();
        private readonly HashSet<string> _actorKeyBuffer = new();

        public Dictionary<string, StoryActorStateData> BuildTargetMap(IReadOnlyList<StoryActorStateData> targetActors)
        {
            _targetActorMap.Clear();

            if (targetActors == null)
                return _targetActorMap;

            for (int i = 0; i < targetActors.Count; i++)
            {
                StoryActorStateData data = targetActors[i];
                if (data == null)
                    continue;

                string id = data.ResolvedActorKey;
                if (!string.IsNullOrWhiteSpace(id))
                    _targetActorMap[id] = data;
            }

            return _targetActorMap;
        }

        public Dictionary<string, StoryActorTrackData> BuildTrackMap(IReadOnlyList<StoryActorTrackData> tracks)
        {
            _actorTrackMap.Clear();

            if (tracks == null)
                return _actorTrackMap;

            for (int i = 0; i < tracks.Count; i++)
            {
                StoryActorTrackData track = tracks[i];
                if (track == null || string.IsNullOrWhiteSpace(track.actorInstanceKey))
                    continue;

                _actorTrackMap[track.actorInstanceKey] = track;
            }

            return _actorTrackMap;
        }

        public float CalculateDuration(
            IReadOnlyDictionary<string, StoryActorStateData> fromMap,
            IReadOnlyDictionary<string, StoryActorStateData> toMap,
            IReadOnlyDictionary<string, StoryActorTrackData> trackMap,
            StoryBackgroundStateData fromBackground,
            StoryBackgroundStateData background,
            StoryBackgroundTrackData backgroundTrack,
            StoryCameraTrackData cameraTrack,
            bool includeCameraTrack)
        {
            float duration = StoryTransitionSampler.BackgroundTransitionDuration(fromBackground, background);
            duration = Mathf.Max(duration, StoryTransitionSampler.GetBackgroundTrackDuration(backgroundTrack));

            _actorKeyBuffer.Clear();
            AddActorKeys(fromMap);
            AddActorKeys(toMap);

            foreach (string key in _actorKeyBuffer)
            {
                TryGetValue(fromMap, key, out StoryActorStateData from);
                TryGetValue(toMap, key, out StoryActorStateData to);

                duration = Mathf.Max(duration, StoryTransitionSampler.ActorTransitionDuration(from, to));

                if (TryGetValue(trackMap, key, out StoryActorTrackData track))
                    duration = Mathf.Max(duration, StoryTransitionSampler.GetActorTrackDuration(track));
            }

            if (includeCameraTrack)
                duration = Mathf.Max(duration, StoryTransitionSampler.GetCameraTrackDuration(cameraTrack));

            return duration;
        }

        public Dictionary<string, StoryActorStateData> SampleActors(
            IReadOnlyDictionary<string, StoryActorStateData> fromMap,
            IReadOnlyDictionary<string, StoryActorStateData> toMap,
            IReadOnlyDictionary<string, StoryActorTrackData> trackMap,
            float elapsed)
        {
            _actorSampleMap.Clear();
            _actorKeyBuffer.Clear();

            AddActorKeys(fromMap);
            AddActorKeys(toMap);

            foreach (string key in _actorKeyBuffer)
            {
                TryGetValue(fromMap, key, out StoryActorStateData from);
                TryGetValue(toMap, key, out StoryActorStateData to);

                StoryActorStateData sample = StoryTransitionSampler.SampleActor(key, from, to, elapsed);
                if (sample == null)
                    continue;

                if (TryGetValue(trackMap, key, out StoryActorTrackData track))
                    sample = StoryTransitionSampler.SampleActorTrackAtTime(sample, track, elapsed);

                if (sample != null)
                    _actorSampleMap[key] = sample;
            }

            return _actorSampleMap;
        }

        public StoryBackgroundStateData SampleBackground(
            StoryBackgroundStateData from,
            StoryBackgroundStateData to,
            StoryBackgroundTrackData track,
            float elapsed)
        {
            StoryBackgroundStateData sample = StoryTransitionSampler.SampleBackground(from, to, elapsed);
            return StoryTransitionSampler.SampleBackgroundTrackAtTime(sample, track, elapsed);
        }

        public bool HasAuthoredCameraTrack(StoryCameraTrackData track)
        {
            if (track == null)
                return false;

            // Any keyframe (CameraOffset, CameraZoom, CameraTarget, etc.) counts as authored.
            if (track.keyframes is { Count: > 0 })
                return true;

            // No keyframes: check defaultState for non-default offset or zoom.
            // Legacy state fields (targetActorInstanceKey, followMode, etc.) are not checked here.
            // CameraTarget is key-only. defaultState contributes only offset and zoom.
            StoryCameraStateData state = track.defaultState;
            if (state == null)
                return false;

            return state.stageLocalPosition != Vector2.zero
                || !Mathf.Approximately(state.zoom, 1f);
        }

        public StoryCameraStateData SampleCameraTrack(StoryCameraTrackData track, float elapsed)
        {
            return StoryTransitionSampler.SampleCameraTrackAtTime(track, "", elapsed);
        }

        private void AddActorKeys<TValue>(IReadOnlyDictionary<string, TValue> map)
        {
            if (map == null)
                return;

            foreach (string key in map.Keys)
                _actorKeyBuffer.Add(key);
        }

        private static bool TryGetValue<TValue>(
            IReadOnlyDictionary<string, TValue> map,
            string key,
            out TValue value)
        {
            if (map != null)
                return map.TryGetValue(key, out value);

            value = default;
            return false;
        }
    }
}
