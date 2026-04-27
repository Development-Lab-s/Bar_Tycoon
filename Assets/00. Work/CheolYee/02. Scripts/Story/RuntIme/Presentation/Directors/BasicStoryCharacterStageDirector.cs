using System.Collections.Generic;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Views;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    public sealed class BasicStoryCharacterStageDirector : MonoBehaviour, ICharacterStageDirector, IStoryStageDirector
    {
        [Header("Roots")]
        [SerializeField] private Transform actorRoot;
        [SerializeField] private Transform backgroundRoot;
        [Header("Deprecated Anchor Fallback")]
        [Tooltip("Deprecated fallback only. Stage layout now uses camera frame coordinates.")]
        [SerializeField] private Transform leftAnchor;
        [Tooltip("Deprecated fallback only. New speaker fallback no longer requires this anchor.")]
        [SerializeField] private Transform centerAnchor;
        [Tooltip("Deprecated fallback only. Stage layout now uses camera frame coordinates.")]
        [SerializeField] private Transform rightAnchor;

        [Header("Shared Visual Prefabs")]
        [SerializeField] private StoryActorVisualView sharedActorPrefab;
        [SerializeField] private StoryBackgroundVisualView sharedBackgroundPrefab;
        [SerializeField] private bool useLegacyCharacterPrefabFallback;
        [SerializeField] private bool useLegacyBackgroundPrefabFallback;
        [SerializeField] private float fallbackAspect = 9f / 16f;
        [SerializeField] private float fallbackCameraWorldWidth = StoryStageVisualSizing.DefaultCameraWorldWidth;

        [Header("Deprecated Default Spawn")]
        [Tooltip("Deprecated. Stage layout normalized position now controls runtime placement.")]
        [SerializeField] private StageAnchorType defaultSpawnAnchor = StageAnchorType.Center;

        [Header("Focus")]
        [SerializeField] private Color focusColor = Color.white;
        [SerializeField] private Color dimColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        private readonly Dictionary<string, ActorEntry> _actors = new();
        private GameObject _backgroundInstance;
        private StoryBackgroundVisualView _backgroundView;
        private string _backgroundKey = "";
        private StoryBackgroundStateData _backgroundState;

        public UniTask EnsureSpeakerVisibleAsync(StoryLineSO line, System.Threading.CancellationToken ct)
        {
            if (line == null || line.Speaker == null)
                return UniTask.CompletedTask;

            string characterId = line.Speaker.CharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
                return UniTask.CompletedTask;

            if (_actors.ContainsKey(characterId))
                return UniTask.CompletedTask;

            Transform parent = actorRoot != null ? actorRoot : transform;
            ActorEntry entry = CreateActorEntry(characterId, line.Speaker, parent);
            if (entry == null)
                return UniTask.CompletedTask;

            entry.CurrentState = new StoryActorStateData
            {
                actor = line.Speaker,
                actorKey = characterId,
                actorInstanceKey = characterId,
                normalizedPosition = new Vector2(0.5f, 0f),
                visible = true,
                focused = false
            };

            ApplyActorEntry(entry, entry.CurrentState);

            _actors[characterId] = entry;
            return UniTask.CompletedTask;
        }

        public void ApplySpeakerFocus(StoryLineSO line)
        {
            string focusedId = line != null && line.Speaker != null
                ? line.Speaker.CharacterId
                : string.Empty;

            foreach (KeyValuePair<string, ActorEntry> pair in _actors)
            {
                bool isFocused = !string.IsNullOrWhiteSpace(focusedId) && pair.Key == focusedId;

                pair.Value.ApplyTint(isFocused ? focusColor : dimColor);

                if (pair.Value.Instance != null)
                {
                    if (isFocused)
                        pair.Value.Instance.transform.SetAsLastSibling();
                }
            }
        }

        public void ClearAll()
        {
            foreach (KeyValuePair<string, ActorEntry> pair in _actors)
            {
                if (pair.Value.Instance != null)
                    Destroy(pair.Value.Instance);
            }

            _actors.Clear();
            if (_backgroundInstance != null)
                Destroy(_backgroundInstance);
            _backgroundInstance = null;
            _backgroundView = null;
            _backgroundKey = "";
            _backgroundState = null;
        }

        // ── IStoryStageDirector ───────────────────────────────────────────────

        public async UniTask ApplyStageLayoutAsync(StoryStageLayoutModuleSO layout, CancellationToken ct)
        {
            if (layout == null)
            {
                await ApplyStageStateAsync(null, ct);
                return;
            }

            var targetMap = BuildTargetMap(layout.Actors);
            var trackMap = BuildTrackMap(layout.ActorTracks);
            var fromMap = BuildCurrentActorStateMap();
            StoryBackgroundStateData fromBackground = _backgroundState != null
                ? _backgroundState.ShallowClone()
                : null;

            float duration = CalculateRuntimeTransitionDuration(fromMap, targetMap, trackMap, fromBackground, layout.Background);
            if (duration <= 0.05f)
            {
                ApplyActorSamples(targetMap);
                ApplyBackgroundState(layout.Background);
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();
                float normalized = Mathf.Clamp01(elapsed / duration);
                var samples = SampleRuntimeActors(fromMap, targetMap, trackMap, elapsed, normalized);
                ApplyActorSamples(samples);
                ApplyBackgroundState(StoryTransitionSampler.SampleBackground(fromBackground, layout.Background, elapsed));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.deltaTime;
            }

            ApplyActorSamples(SampleRuntimeActors(fromMap, targetMap, trackMap, duration, 1f));
            ApplyBackgroundState(layout.Background);
        }

        public UniTask ApplyStageStateAsync(IReadOnlyList<StoryActorStateData> targetActors, CancellationToken ct)
        {
            var targetMap = BuildTargetMap(targetActors);
            ApplyActorSamples(targetMap);
            return UniTask.CompletedTask;
        }

        private Dictionary<string, StoryActorStateData> BuildTargetMap(IReadOnlyList<StoryActorStateData> targetActors)
        {
            var targetMap = new Dictionary<string, StoryActorStateData>();
            if (targetActors == null)
                return targetMap;

            foreach (var data in targetActors)
            {
                if (data == null) continue;
                string id = data.ResolvedActorKey;
                if (!string.IsNullOrWhiteSpace(id))
                    targetMap[id] = data;
            }

            return targetMap;
        }

        private static Dictionary<string, StoryActorTrackData> BuildTrackMap(IReadOnlyList<StoryActorTrackData> tracks)
        {
            var result = new Dictionary<string, StoryActorTrackData>();
            if (tracks == null)
                return result;

            foreach (StoryActorTrackData track in tracks)
            {
                if (track == null || string.IsNullOrWhiteSpace(track.actorInstanceKey))
                    continue;

                result[track.actorInstanceKey] = track;
            }

            return result;
        }

        private Dictionary<string, StoryActorStateData> BuildCurrentActorStateMap()
        {
            var result = new Dictionary<string, StoryActorStateData>();
            foreach (var pair in _actors)
            {
                if (pair.Value.CurrentState != null)
                    result[pair.Key] = pair.Value.CurrentState.ShallowClone();
            }

            return result;
        }

        private float CalculateRuntimeTransitionDuration(
            Dictionary<string, StoryActorStateData> fromMap,
            Dictionary<string, StoryActorStateData> toMap,
            Dictionary<string, StoryActorTrackData> trackMap,
            StoryBackgroundStateData fromBackground,
            StoryBackgroundStateData background)
        {
            float duration = StoryTransitionSampler.BackgroundTransitionDuration(fromBackground, background);
            var keys = new HashSet<string>(fromMap.Keys);
            keys.UnionWith(toMap.Keys);

            foreach (string key in keys)
            {
                fromMap.TryGetValue(key, out var from);
                toMap.TryGetValue(key, out var to);
                duration = Mathf.Max(duration, StoryTransitionSampler.ActorTransitionDuration(from, to));
                if (trackMap.TryGetValue(key, out var track))
                    duration = Mathf.Max(duration, StoryTransitionSampler.GetActorTrackDuration(track));
            }

            return duration;
        }

        private Dictionary<string, StoryActorStateData> SampleRuntimeActors(
            Dictionary<string, StoryActorStateData> fromMap,
            Dictionary<string, StoryActorStateData> toMap,
            Dictionary<string, StoryActorTrackData> trackMap,
            float elapsed,
            float normalized)
        {
            var samples = new Dictionary<string, StoryActorStateData>();
            var keys = new HashSet<string>(fromMap.Keys);
            keys.UnionWith(toMap.Keys);

            foreach (string key in keys)
            {
                fromMap.TryGetValue(key, out var from);
                toMap.TryGetValue(key, out var to);
                StoryActorStateData sample = StoryTransitionSampler.SampleActor(key, from, to, elapsed);
                if (sample == null)
                    continue;

                if (trackMap.TryGetValue(key, out var track))
                    sample = StoryTransitionSampler.SampleActorTrackAtTime(sample, track, elapsed);

                if (sample != null)
                    samples[key] = sample;
            }

            return samples;
        }

        private void ApplyActorSamples(Dictionary<string, StoryActorStateData> targetMap)
        {
            // 타겟에 없는 액터 퇴장
            var toExit = new List<string>();
            foreach (var id in _actors.Keys)
                if (!targetMap.ContainsKey(id)) toExit.Add(id);
            foreach (var id in toExit)
            {
                if (_actors.TryGetValue(id, out var e) && e.Instance != null)
                    Destroy(e.Instance);
                _actors.Remove(id);
            }

            // 타겟 액터 등장 / 이동 / 포커스 적용
            Transform parent = actorRoot != null ? actorRoot : transform;
            foreach (var kvp in targetMap)
            {
                var data = kvp.Value;
                var charId = kvp.Key;

                if (!_actors.ContainsKey(charId))
                {
                    // 신규 등장
                    var entry = CreateActorEntry(charId, data.actor, parent);
                    if (entry == null) continue;
                    _actors[charId] = entry;
                }

                ApplyActorEntry(_actors[charId], data);
            }
        }

        private ActorEntry CreateActorEntry(string actorKey, CharacterDefinitionSO character, Transform parent)
        {
            GameObject instance = null;
            StoryActorVisualView view = null;

            if (sharedActorPrefab != null)
            {
                view = Instantiate(sharedActorPrefab, parent);
                instance = view.gameObject;
            }
            else if (useLegacyCharacterPrefabFallback && character != null && character.DefaultActorPrefab != null)
            {
                instance = Instantiate(character.DefaultActorPrefab, parent);
                view = instance.GetComponent<StoryActorVisualView>();
                if (view == null)
                    view = instance.AddComponent<StoryActorVisualView>();
            }
            else
            {
                instance = new GameObject(string.IsNullOrWhiteSpace(actorKey) ? "Story Actor" : $"Story Actor {actorKey}");
                instance.transform.SetParent(parent, false);
                view = instance.AddComponent<StoryActorVisualView>();
            }

            return instance != null ? new ActorEntry(instance, view, character) : null;
        }

        private void ApplyActorEntry(ActorEntry actorEntry, StoryActorStateData data)
        {
            if (actorEntry == null || actorEntry.Instance == null || data == null)
                return;

            actorEntry.Instance.transform.position = NormPosToWorld(data);
            float focusBlend = StoryTransitionSampler.ResolveFocusBlend(data.EffectiveFocusAlpha);
            Color tint = Color.Lerp(dimColor, focusColor, focusBlend);
            if (actorEntry.View != null)
                actorEntry.View.Apply(data, tint);
            else
            {
                actorEntry.Instance.SetActive(data.visible);
                actorEntry.ApplyTint(tint);
            }

            actorEntry.CurrentState = data.ShallowClone();
        }

        private Vector3 NormPosToWorld(StoryActorStateData data)
        {
            return ResolveStageCameraMetrics().ActorPosition(data.normalizedPosition, data.EffectiveOffset, 0f);
        }

        private StoryStageCameraMetrics ResolveStageCameraMetrics()
        {
            Camera camera = Camera.main;
            if (camera != null && camera.orthographic)
                return StoryStageCameraMetrics.FromOrthographicCamera(camera);

            float aspect = camera != null ? camera.aspect : fallbackAspect;
            if (leftAnchor != null && rightAnchor != null)
                return new StoryStageCameraMetrics(leftAnchor.position, rightAnchor.position, aspect);

            Vector3 center = transform.position;
            return StoryStageCameraMetrics.FromCenteredFrame(center, fallbackCameraWorldWidth, aspect);
        }

        private void ApplyBackgroundState(StoryBackgroundStateData state)
        {
            if (state == null || !state.HasBackground || !state.visible)
            {
                if (_backgroundInstance != null)
                    _backgroundInstance.SetActive(false);
                _backgroundState = state != null ? state.ShallowClone() : null;
                return;
            }

            string key = state.ResolvedBackgroundKey;
            if (_backgroundInstance == null || _backgroundKey != key)
                RecreateBackgroundInstance(state, key);

            if (_backgroundInstance == null)
                return;

            _backgroundInstance.SetActive(true);
            if (_backgroundView != null)
            {
                StoryStageCameraMetrics camera = ResolveStageCameraMetrics();
                _backgroundView.Apply(state, camera);
            }

            _backgroundState = state.ShallowClone();
        }

        private void RecreateBackgroundInstance(StoryBackgroundStateData state, string key)
        {
            if (_backgroundInstance != null)
                Destroy(_backgroundInstance);

            Transform parent = backgroundRoot != null ? backgroundRoot : transform;
            if (sharedBackgroundPrefab != null)
            {
                _backgroundView = Instantiate(sharedBackgroundPrefab, parent);
                _backgroundInstance = _backgroundView.gameObject;
            }
            else if (useLegacyBackgroundPrefabFallback && state.background != null && state.background.RuntimePrefab != null)
            {
                _backgroundInstance = Instantiate(state.background.RuntimePrefab, parent);
                _backgroundView = _backgroundInstance.GetComponent<StoryBackgroundVisualView>();
                if (_backgroundView == null)
                    _backgroundView = _backgroundInstance.AddComponent<StoryBackgroundVisualView>();
            }
            else
            {
                _backgroundInstance = new GameObject("Story Background");
                _backgroundInstance.transform.SetParent(parent, false);
                _backgroundView = _backgroundInstance.AddComponent<StoryBackgroundVisualView>();
            }

            _backgroundKey = key;
        }

        // ─────────────────────────────────────────────────────────────────────

        private Transform GetAnchor(StageAnchorType anchorType)
        {
            return anchorType switch
            {
                StageAnchorType.Left => leftAnchor,
                StageAnchorType.Center => centerAnchor,
                StageAnchorType.Right => rightAnchor,
                _ => centerAnchor,
            };
        }

        private sealed class ActorEntry
        {
            public GameObject Instance { get; }
            public StoryActorVisualView View { get; }
            public StoryActorStateData CurrentState { get; set; }
            private readonly SpriteRenderer[] _spriteRenderers;

            public ActorEntry(GameObject instance, StoryActorVisualView view, CharacterDefinitionSO character)
            {
                Instance = instance;
                View = view;
                _spriteRenderers = instance != null
                    ? instance.GetComponentsInChildren<SpriteRenderer>(true)
                    : new SpriteRenderer[0];
            }

            public void ApplyTint(Color color)
            {
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    if (_spriteRenderers[i] != null)
                        _spriteRenderers[i].color = color;
                }
            }
        }
    }
}
