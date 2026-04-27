using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    [CreateAssetMenu(fileName = "BackgroundDefinition", menuName = "Story/Background Definition")]
    public sealed class BackgroundDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string backgroundId;
        [SerializeField] private string displayName;

        [Header("Presentation")]
        [SerializeField] private Sprite previewSprite;
        [SerializeField] private GameObject runtimePrefab;

        [Header("Stage Defaults")]
        [SerializeField] private Vector2 defaultScale = Vector2.one;
        [SerializeField] private Vector2 defaultNormalizedOffset = Vector2.zero;
        [SerializeField] private Vector2 defaultPivot = new Vector2(0.5f, 0.5f);
        [SerializeField] private Color defaultTint = Color.white;
        [SerializeField] [Range(0f, 1f)] private float defaultOpacity = 1f;
        [SerializeField] private int defaultSortOrder = -100;

        public string BackgroundId => backgroundId;
        public string DisplayName => displayName;
        public Sprite PreviewSprite => previewSprite;
        public GameObject RuntimePrefab => runtimePrefab;
        public Vector2 DefaultScale => defaultScale;
        public Vector2 DefaultNormalizedOffset => defaultNormalizedOffset;
        public Vector2 DefaultPivot => defaultPivot;
        public Color DefaultTint => defaultTint;
        public float DefaultOpacity => defaultOpacity;
        public int DefaultSortOrder => defaultSortOrder;
    }
}
