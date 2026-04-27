using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    [Serializable]
    public sealed class CharacterExpressionSpriteData
    {
        [Tooltip("Enum expression used by preview/runtime. Legacy string keys remain as fallback.")]
        public StoryExpressionType expression = StoryExpressionType.Neutral;

        [HideInInspector]
        public string expressionKey = "";

        public Sprite fullBodySprite;
    }

    /// <summary>
    /// 스토리 시스템에서 사용되는 캐릭터의 정의를 담는 ScriptableObject 클래스입니다.
    /// 캐릭터의 ID, 이름, 아이콘, 기본 프리팹 등의 정보를 포함합니다.
    /// </summary>
    
    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Story/Character Definition")]
    public sealed class CharacterDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string characterId; 
        //스토리 시스템 내에서 캐릭터를 고유하게 식별하는 ID입니다.
        //대사나 이벤트에서 이 ID를 참조하여 캐릭터를 지정합니다.
        [SerializeField] private string displayName;
        //스토리 진행 중 캐릭터의 이름을 표시할 때 사용하는 문자열입니다.

        [Header("Presentation")]
        [SerializeField] private Sprite logIcon;
        //스토리 로그나 UI에서 캐릭터를 시각적으로 나타낼 때 사용하는 아이콘입니다.
        [SerializeField] private GameObject defaultActorPrefab;
        //스토리 진행 중 캐릭터가 화면에 등장할 때 사용하는 기본 프리팹입니다.
        //이 프리팹은 캐릭터의 시각적 표현과 애니이션을 담당할 수 있습니다.

        [Header("Preview")]
        [Tooltip("StoryPreviewWindow 에서 표시할 스프라이트. 없으면 LogIcon 사용.")]
        [SerializeField] private Sprite previewSprite;

        [Header("Shared Actor Visual")]
        [Tooltip("Default full-body sprite used by the shared actor view. Falls back to PreviewSprite/LogIcon.")]
        [SerializeField] private Sprite defaultFullBodySprite;
        [Tooltip("Full-body sprite variants keyed by expression enum. Legacy string keys remain as fallback.")]
        [SerializeField] private List<CharacterExpressionSpriteData> expressionSprites = new();
        [Tooltip("Default expression used when a line state has no explicit expression override.")]
        [SerializeField] private StoryExpressionType defaultExpression = StoryExpressionType.Neutral;
        [Tooltip("Target sprite height in story camera world units before line scale is applied.")]
        [SerializeField] private float baseWorldHeight = 3f;
        [Tooltip("Character-wide visual scale multiplier applied before line scale.")]
        [SerializeField] private float defaultScaleMultiplier = 1f;
        [Tooltip("Keep actor sprites from being stretched by non-uniform line scale.")]
        [SerializeField] private bool preserveAspectRatio = true;

        [Header("Stage Defaults")]
        [SerializeField] private Vector2 defaultStageScale = Vector2.one;
        [SerializeField] private Vector2 defaultStageOffset = Vector2.zero;
        [SerializeField] private Vector2 defaultStagePivot = new Vector2(0.5f, 0f);
        [SerializeField] private StoryActorMotionProfileData defaultMotionProfile = new();

        //캐릭터 정의의 각 필드에 대한 공개 읽기 전용 프로퍼티입니다.
        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public Sprite LogIcon => logIcon;
        public GameObject DefaultActorPrefab => defaultActorPrefab;
        public GameObject ActorPrefab => defaultActorPrefab;
        public Sprite PreviewSprite => previewSprite != null ? previewSprite : logIcon;
        public Sprite DefaultFullBodySprite => defaultFullBodySprite != null ? defaultFullBodySprite : PreviewSprite;
        public IReadOnlyList<CharacterExpressionSpriteData> ExpressionSprites => expressionSprites;
        public StoryExpressionType DefaultExpression => defaultExpression;
        public float BaseWorldHeight => Mathf.Max(0.01f, baseWorldHeight);
        public float DefaultScaleMultiplier => Mathf.Approximately(defaultScaleMultiplier, 0f) ? 1f : defaultScaleMultiplier;
        public bool PreserveAspectRatio => preserveAspectRatio;
        public Vector2 DefaultStageScale => defaultStageScale;
        public Vector2 DefaultStageOffset => defaultStageOffset;
        public Vector2 DefaultStagePivot => defaultStagePivot;
        public StoryActorMotionProfileData DefaultMotionProfile => defaultMotionProfile;

        public Sprite ResolveExpressionSprite(StoryExpressionType expression) =>
            ResolveExpressionSprite(expression, string.Empty);

        public Sprite ResolveExpressionSprite(StoryExpressionType expression, string legacyExpressionKey)
        {
            if (!string.IsNullOrWhiteSpace(legacyExpressionKey)
                && expression == defaultExpression
                && TryResolveLegacyExpressionSprite(legacyExpressionKey, out Sprite legacySprite))
                return legacySprite;

            if (TryGetExpressionSprite(expression, out Sprite sprite))
                return sprite;

            if (TryResolveLegacyExpressionSprite(legacyExpressionKey, out legacySprite))
                return legacySprite;

            return DefaultFullBodySprite;
        }

        public Sprite ResolveExpressionSprite(string expressionKey)
        {
            if (TryParseExpression(expressionKey, out StoryExpressionType expression))
                return ResolveExpressionSprite(expression, expressionKey);

            return TryResolveLegacyExpressionSprite(expressionKey, out Sprite sprite)
                ? sprite
                : DefaultFullBodySprite;
        }

        public bool TryGetExpressionSprite(StoryExpressionType expression, out Sprite sprite)
        {
            sprite = null;
            if (expressionSprites == null)
                return false;

            foreach (CharacterExpressionSpriteData entry in expressionSprites)
            {
                if (entry == null || entry.fullBodySprite == null)
                    continue;

                if (MatchesExpression(entry, expression))
                {
                    sprite = entry.fullBodySprite;
                    return true;
                }
            }

            return false;
        }

        private bool TryResolveLegacyExpressionSprite(string expressionKey, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(expressionKey) || expressionSprites == null)
                return false;

            foreach (CharacterExpressionSpriteData entry in expressionSprites)
            {
                if (entry == null || entry.fullBodySprite == null)
                    continue;

                if (string.Equals(entry.expressionKey, expressionKey, StringComparison.Ordinal))
                {
                    sprite = entry.fullBodySprite;
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesExpression(CharacterExpressionSpriteData entry, StoryExpressionType expression)
        {
            if (entry == null)
                return false;

            if (!string.IsNullOrWhiteSpace(entry.expressionKey))
                return TryParseExpression(entry.expressionKey, out StoryExpressionType parsed) && parsed == expression;

            return entry.expression == expression;
        }

        private static bool TryParseExpression(string value, out StoryExpressionType expression) =>
            Enum.TryParse(value, true, out expression);
    }
}
