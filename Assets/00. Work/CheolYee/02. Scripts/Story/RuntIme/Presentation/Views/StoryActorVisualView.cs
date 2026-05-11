using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Views
{
    public sealed class StoryActorVisualView : MonoBehaviour
    {
        private const string GeneratedSpriteRootName = "Sprite";

        [SerializeField] private Transform spriteRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Range(0f, 1f)] private float expressionCrossFadeDuration = 0.3f;

        private StoryActorStateData _currentState;
        private Color _currentTint = Color.white;
        private SpriteRenderer _fadeRenderer;
        private float _fadeRemaining;
        private float _fadeDuration;
        private bool _warnedAboutRootRenderer;
        private bool _warnedAboutUnsafePrefabScale;

        private void Awake()
        {
            ValidateInitialPrefabScale();
        }

        private void OnValidate()
        {
            ValidateInitialPrefabScale();
        }

        public void Apply(StoryActorStateData state, Color tint)
        {
            if (state == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _currentState = state.ShallowClone();
            _currentTint = tint;
            EnsureRenderer();
            Sprite sprite = StoryStageVisualSizing.ResolveActorSprite(state);
            ApplySprite(sprite, tint);
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spriteRenderer.sortingOrder = state.sortOrder;
            spriteRenderer.color = tint;
            Transform renderRoot = spriteRoot != null ? spriteRoot : spriteRenderer.transform;
            if (renderRoot != transform)
                renderRoot.localPosition = StoryStageVisualSizing.CalculatePivotLocalOffset(sprite, state.EffectivePivot);
            if (_fadeRenderer != null)
                _fadeRenderer.transform.localPosition = renderRoot.localPosition;
            transform.localScale = StoryStageVisualSizing.CalculateActorWorldScale(state, sprite);
            gameObject.SetActive(state.visible);
        }

        public void SetExpression(StoryExpressionType expression)
        {
            if (_currentState == null)
                return;

            _currentState.expression = expression;
            _currentState.expressionKey = string.Empty;
            Apply(_currentState, _currentTint);
        }

        public bool TryGetExpressionSprite(StoryExpressionType expression, out Sprite sprite)
        {
            sprite = null;
            if (_currentState == null || _currentState.actor == null)
                return false;

            sprite = _currentState.actor.ResolveExpressionSprite(expression);
            return sprite != null;
        }

        public void ApplyTint(Color tint)
        {
            _currentTint = tint;
            EnsureRenderer();
            spriteRenderer.color = tint;
        }

        private void EnsureRenderer()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

            if (spriteRenderer != null && spriteRenderer.transform == transform)
                MoveRootRendererToVisualChild();

            if (spriteRenderer == null)
            {
                var child = new GameObject("Sprite");
                child.transform.SetParent(transform, false);
                spriteRenderer = child.AddComponent<SpriteRenderer>();
            }

            if (spriteRoot == null)
                spriteRoot = spriteRenderer.transform;
        }

        private void Update()
        {
            if (_fadeRenderer == null || _fadeRemaining <= 0f)
                return;

            _fadeRemaining -= Time.deltaTime;
            float alpha = _fadeDuration > 0f ? Mathf.Clamp01(_fadeRemaining / _fadeDuration) : 0f;
            Color color = _currentTint;
            color.a *= alpha;
            _fadeRenderer.color = color;
            if (_fadeRemaining <= 0f)
                _fadeRenderer.enabled = false;
        }

        private void ApplySprite(Sprite sprite, Color tint)
        {
            if (spriteRenderer.sprite != null
                && spriteRenderer.sprite != sprite
                && expressionCrossFadeDuration > 0f)
            {
                EnsureFadeRenderer();
                if (_fadeRenderer != null)
                {
                    _fadeRenderer.sprite = spriteRenderer.sprite;
                    _fadeRenderer.drawMode = SpriteDrawMode.Simple;
                    _fadeRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
                    _fadeRenderer.color = tint;
                    _fadeRenderer.enabled = true;
                    _fadeDuration = expressionCrossFadeDuration;
                    _fadeRemaining = expressionCrossFadeDuration;
                }
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = tint;
        }

        private void EnsureFadeRenderer()
        {
            EnsureRenderer();
            if (_fadeRenderer != null)
                return;

            var child = new GameObject("PreviousSprite");
            child.transform.SetParent(spriteRoot != null ? spriteRoot.parent : transform, false);
            child.transform.localPosition = spriteRoot != null ? spriteRoot.localPosition : Vector3.zero;
            child.transform.SetSiblingIndex(spriteRoot != null ? spriteRoot.GetSiblingIndex() : 0);
            _fadeRenderer = child.AddComponent<SpriteRenderer>();
            CopyRendererSettings(spriteRenderer, _fadeRenderer);
        }

        private void MoveRootRendererToVisualChild()
        {
            if (!_warnedAboutRootRenderer)
            {
                Debug.LogWarning(
                    $"{nameof(StoryActorVisualView)} on '{name}' has a SpriteRenderer on the root. " +
                    "Use a child Visual/Sprite object for stable pivot offsets. Runtime will create one for this instance.",
                    this);
                _warnedAboutRootRenderer = true;
            }

            SpriteRenderer rootRenderer = spriteRenderer;
            Transform child = transform.Find(GeneratedSpriteRootName);
            if (child == null)
            {
                var childObject = new GameObject(GeneratedSpriteRootName);
                childObject.transform.SetParent(transform, false);
                child = childObject.transform;
            }

            SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();
            if (childRenderer == null)
                childRenderer = child.gameObject.AddComponent<SpriteRenderer>();

            CopyRendererSettings(rootRenderer, childRenderer);
            rootRenderer.enabled = false;
            spriteRoot = child;
            spriteRenderer = childRenderer;
        }

        private static void CopyRendererSettings(SpriteRenderer source, SpriteRenderer target)
        {
            if (source == null || target == null || source == target)
                return;

            target.sharedMaterial = source.sharedMaterial;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder;
            target.maskInteraction = source.maskInteraction;
            target.flipX = source.flipX;
            target.flipY = source.flipY;
            target.color = source.color;
        }

        private void ValidateInitialPrefabScale()
        {
            if (_warnedAboutUnsafePrefabScale)
                return;

            Vector3 scale = transform.localScale;
            if (Mathf.Approximately(scale.x, 1f)
                && Mathf.Approximately(scale.y, 1f)
                && Mathf.Approximately(scale.z, 1f))
                return;

            Debug.LogWarning(
                $"{nameof(StoryActorVisualView)} on '{name}' should use root scale (1,1,1). " +
                "Story scale is applied from CharacterDefinitionSO and StoryActorStateData at runtime.",
                this);
            _warnedAboutUnsafePrefabScale = true;
        }
    }
}
