using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    public sealed class BasicStoryCharacterStageDirector : MonoBehaviour, ICharacterStageDirector
    {
        [Header("Roots")]
        [SerializeField] private Transform actorRoot;
        [SerializeField] private Transform leftAnchor;
        [SerializeField] private Transform centerAnchor;
        [SerializeField] private Transform rightAnchor;

        [Header("Default Spawn")]
        [SerializeField] private StageAnchorType defaultSpawnAnchor = StageAnchorType.Center;

        [Header("Focus")]
        [SerializeField] private Color focusColor = Color.white;
        [SerializeField] private Color dimColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        private readonly Dictionary<string, ActorEntry> _actors = new();

        public UniTask EnsureSpeakerVisibleAsync(StoryLineSO line, System.Threading.CancellationToken ct)
        {
            if (line == null || line.Speaker == null)
                return UniTask.CompletedTask;

            string characterId = line.Speaker.CharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
                return UniTask.CompletedTask;

            if (_actors.ContainsKey(characterId))
                return UniTask.CompletedTask;

            GameObject prefab = line.Speaker.DefaultActorPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"Character '{line.Speaker.name}' has no default actor prefab.");
                return UniTask.CompletedTask;
            }

            Transform parent = actorRoot != null ? actorRoot : transform;
            Transform anchor = GetAnchor(defaultSpawnAnchor);

            GameObject instance = Instantiate(prefab, parent);

            if (anchor != null)
            {
                instance.transform.position = anchor.position;
                instance.transform.rotation = anchor.rotation;
            }

            ActorEntry entry = new ActorEntry(instance, line.Speaker);
            entry.ApplyTint(dimColor);

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
        }

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
            private readonly SpriteRenderer[] _spriteRenderers;

            public ActorEntry(GameObject instance, CharacterDefinitionSO character)
            {
                Instance = instance;
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