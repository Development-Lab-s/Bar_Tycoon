using System;
using System.Collections.Generic;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Views;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    /// <summary>
    /// Owns runtime actor instances for the story stage.
    /// Actor creation, caching, removal, focus tint, and state application stay here.
    /// </summary>
    public sealed class StoryActorStageRuntime : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private Transform actorRoot;

        [Header("Shared Visual Prefab")]
        [SerializeField] private StoryActorVisualView sharedActorPrefab;

        [Header("Legacy Fallback")]
        [SerializeField] private bool useLegacyCharacterPrefabFallback;

        [Header("Focus")]
        [SerializeField] private Color focusColor = Color.white;
        [SerializeField] private Color dimColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        private readonly Dictionary<string, ActorEntry> _actors = new();
        private readonly List<string> _removeBuffer = new();

        public bool HasActor(string actorKey)
        {
            return !string.IsNullOrWhiteSpace(actorKey) && _actors.ContainsKey(actorKey);
        }

        public bool TryGetActorWorldPosition(string actorKey, out Vector3 position)
        {
            position = default;

            if (string.IsNullOrWhiteSpace(actorKey))
                return false;

            if (!_actors.TryGetValue(actorKey, out ActorEntry entry))
                return false;

            if (entry?.Instance == null)
                return false;

            position = entry.Instance.transform.position;
            return true;
        }

        public UniTask EnsureSpeakerVisibleAsync(
            StoryLineSO line,
            CancellationToken ct)
        {
            if (line == null || line.Speaker == null)
                return UniTask.CompletedTask;

            string characterId = line.Speaker.CharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
                return UniTask.CompletedTask;

            if (_actors.ContainsKey(characterId))
                return UniTask.CompletedTask;

            ActorEntry entry = CreateActorEntry(characterId, line.Speaker, ResolveActorRoot());
            if (entry == null)
                return UniTask.CompletedTask;

            StoryActorStateData state = new()
            {
                actor = line.Speaker,
                actorKey = characterId,
                actorInstanceKey = characterId,
                stageLocalPosition = Vector2.zero,
                visible = true,
                focused = false
            };

            ApplyActorEntry(entry, state);
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

                if (isFocused && pair.Value.Instance != null)
                    pair.Value.Instance.transform.SetAsLastSibling();
            }
        }

        public Dictionary<string, StoryActorStateData> BuildCurrentActorStateMap()
        {
            Dictionary<string, StoryActorStateData> result = new();

            foreach (KeyValuePair<string, ActorEntry> pair in _actors)
            {
                if (pair.Value.CurrentState != null)
                    result[pair.Key] = pair.Value.CurrentState.ShallowClone();
            }

            return result;
        }

        public void ApplySamples(Dictionary<string, StoryActorStateData> targetMap)
        {
            targetMap ??= new Dictionary<string, StoryActorStateData>();

            RemoveMissingActors(targetMap);
            ApplyTargetActors(targetMap);
        }

        public void ClearAll()
        {
            foreach (KeyValuePair<string, ActorEntry> pair in _actors)
            {
                if (pair.Value.Instance != null)
                    Destroy(pair.Value.Instance);
            }

            _actors.Clear();
            _removeBuffer.Clear();
        }

        private void RemoveMissingActors(Dictionary<string, StoryActorStateData> targetMap)
        {
            _removeBuffer.Clear();

            foreach (string actorKey in _actors.Keys)
            {
                if (!targetMap.ContainsKey(actorKey))
                    _removeBuffer.Add(actorKey);
            }

            for (int i = 0; i < _removeBuffer.Count; i++)
            {
                string actorKey = _removeBuffer[i];

                if (_actors.TryGetValue(actorKey, out ActorEntry entry) && entry.Instance != null)
                    Destroy(entry.Instance);

                _actors.Remove(actorKey);
            }
        }

        private void ApplyTargetActors(Dictionary<string, StoryActorStateData> targetMap)
        {
            Transform parent = ResolveActorRoot();

            foreach (KeyValuePair<string, StoryActorStateData> pair in targetMap)
            {
                string actorKey = pair.Key;
                StoryActorStateData data = pair.Value;

                if (data == null)
                    continue;

                if (!_actors.TryGetValue(actorKey, out ActorEntry entry))
                {
                    entry = CreateActorEntry(actorKey, data.actor, parent);
                    if (entry == null)
                        continue;

                    _actors[actorKey] = entry;
                }

                ApplyActorEntry(entry, data);
            }
        }

        private ActorEntry CreateActorEntry(
            string actorKey,
            CharacterDefinitionSO character,
            Transform parent)
        {
            GameObject instance;
            StoryActorVisualView view;

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
                instance = new GameObject(string.IsNullOrWhiteSpace(actorKey)
                    ? "Story Actor"
                    : $"Story Actor - {actorKey}");

                instance.transform.SetParent(parent, false);
                view = instance.AddComponent<StoryActorVisualView>();
            }

            if (instance != null)
                instance.name = string.IsNullOrWhiteSpace(actorKey)
                    ? "Story Actor"
                    : $"Story Actor - {actorKey}";

            return instance != null ? new ActorEntry(instance, view) : null;
        }

        private void ApplyActorEntry(ActorEntry actorEntry, StoryActorStateData data)
        {
            if (actorEntry == null || actorEntry.Instance == null || data == null)
                return;

            Transform root = ResolveActorRoot();
            Vector3 rootCenter = root != null ? root.position : Vector3.zero;
            rootCenter.z = 0f;
            actorEntry.Instance.transform.position = StoryActorStageTransformCalculator.WorldPosition(
                data.stageLocalPosition,
                rootCenter,
                0f);

            float focusBlend = StoryTransitionSampler.ResolveFocusBlend(data.EffectiveFocusAlpha);
            Color tint = Color.Lerp(dimColor, focusColor, focusBlend);

            if (actorEntry.View != null)
            {
                actorEntry.View.Apply(data, tint);
            }
            else
            {
                actorEntry.Instance.SetActive(data.visible);
                actorEntry.ApplyTint(tint);
            }

            if (data.focused && actorEntry.Instance != null)
                actorEntry.Instance.transform.SetAsLastSibling();

            actorEntry.CurrentState = data.ShallowClone();
        }

        private Transform ResolveActorRoot()
        {
            return actorRoot != null ? actorRoot : transform;
        }

        private sealed class ActorEntry
        {
            public GameObject Instance { get; }
            public StoryActorVisualView View { get; }
            public StoryActorStateData CurrentState { get; set; }

            private readonly SpriteRenderer[] _spriteRenderers;

            public ActorEntry(GameObject instance, StoryActorVisualView view)
            {
                Instance = instance;
                View = view;
                _spriteRenderers = instance != null
                    ? instance.GetComponentsInChildren<SpriteRenderer>(true)
                    : Array.Empty<SpriteRenderer>();
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
