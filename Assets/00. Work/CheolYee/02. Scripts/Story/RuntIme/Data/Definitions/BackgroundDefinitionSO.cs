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
        [Tooltip("Legacy per-background prefab fallback. Runtime now prefers the shared background prefab on the stage director.")]
        [SerializeField] private GameObject runtimePrefab;

        [Header("Stage Defaults")]
        [SerializeField] private Vector2 defaultScale = Vector2.one;
        [SerializeField] private Vector2 defaultNormalizedOffset = Vector2.zero;
        [SerializeField] private Vector2 defaultPivot = new Vector2(0.5f, 0.5f);
        [Tooltip("Extra cover scale applied after fitting the background over the camera frame.")]
        [SerializeField] private float defaultCoverOverscan = 1.08f;
        [Tooltip("Keep background sprites uniform while covering the camera frame.")]
        [SerializeField] private bool preserveAspectRatio = true;
        [Tooltip("Reserved for future parallax. 0 means locked to camera.")]
        [SerializeField] [Range(0f, 1f)] private float parallaxFactor = 0f;
        [SerializeField] private Color defaultTint = Color.white;
        [SerializeField] [Range(0f, 1f)] private float defaultOpacity = 1f;
        [SerializeField] private int defaultSortOrder = -100;

        public string BackgroundId => backgroundId;
        public string DisplayName => displayName;
        public Sprite PreviewSprite => previewSprite;
        public Sprite DefaultSprite => previewSprite;
        public GameObject RuntimePrefab => runtimePrefab;
        public Vector2 DefaultScale => defaultScale;
        public Vector2 DefaultNormalizedOffset => defaultNormalizedOffset;
        public Vector2 DefaultPivot => defaultPivot;
        public float DefaultCoverOverscan => Mathf.Max(1f, defaultCoverOverscan);
        public bool PreserveAspectRatio => preserveAspectRatio;
        public float ParallaxFactor => Mathf.Clamp01(parallaxFactor);
        public Color DefaultTint => defaultTint;
        public float DefaultOpacity => defaultOpacity;
        public int DefaultSortOrder => defaultSortOrder;
    }
}
