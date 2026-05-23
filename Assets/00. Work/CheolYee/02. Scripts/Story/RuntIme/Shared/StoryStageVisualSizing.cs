using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared
{
    public readonly struct StoryStageCameraMetrics
    {
        public StoryStageCameraMetrics(Vector3 leftBottom, Vector3 rightBottom, float aspect)
        {
            LeftBottom = leftBottom;
            RightBottom = rightBottom;
            Aspect = Mathf.Max(0.1f, aspect);
            Width = Mathf.Max(0.0001f, Vector3.Distance(leftBottom, rightBottom));
            Height = Width / Aspect;
            Vector3 right = rightBottom - leftBottom;
            RightDirection = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
            Vector3 bottomCenter = Vector3.Lerp(leftBottom, rightBottom, 0.5f);
            Center = bottomCenter + Vector3.up * (Height * 0.5f);
        }

        public Vector3 LeftBottom { get; }
        public Vector3 RightBottom { get; }
        public Vector3 RightDirection { get; }
        public Vector3 Center { get; }
        public float Width { get; }
        public float Height { get; }
        public float Aspect { get; }

        public static StoryStageCameraMetrics FromOrthographicCamera(UnityEngine.Camera camera, float z = 0f)
        {
            if (camera == null || !camera.orthographic)
                return FromCenteredFrame(Vector3.zero, StoryStageVisualSizing.DefaultCameraWorldWidth, 9f / 16f, z);

            float height = Mathf.Max(0.0001f, camera.orthographicSize * 2f);
            float aspect = Mathf.Max(0.1f, camera.aspect);
            float width = height * aspect;
            Vector3 center = camera.transform.position;
            center.z = z;
            return FromCenteredFrame(center, width, aspect, z);
        }

        public static StoryStageCameraMetrics FromCenteredFrame(Vector3 center, float width, float aspect, float z = 0f)
        {
            float safeAspect = Mathf.Max(0.1f, aspect);
            float safeWidth = Mathf.Max(0.0001f, width);
            float height = safeWidth / safeAspect;
            center.z = z;
            Vector3 leftBottom = center - Vector3.right * (safeWidth * 0.5f) - Vector3.up * (height * 0.5f);
            Vector3 rightBottom = center + Vector3.right * (safeWidth * 0.5f) - Vector3.up * (height * 0.5f);
            return new StoryStageCameraMetrics(leftBottom, rightBottom, safeAspect);
        }
    }

    public static class StoryStageVisualSizing
    {
        public const float DefaultCameraWorldWidth = 6f;
        private const float FallbackActorAspectHeightOverWidth = 1.8f;

        public static Sprite ResolveActorSprite(StoryActorStateData state)
        {
            CharacterDefinitionSO actor = state != null ? state.actor : null;
            return actor != null
                ? actor.ResolveExpressionSprite(state.ResolvedExpression, state.expressionKey)
                : null;
        }

        public static Sprite ResolveBackgroundSprite(StoryBackgroundStateData state) =>
            state != null && state.background != null
                ? state.background.DefaultSprite
                : null;

        public static Vector3 CalculateActorWorldScale(StoryActorStateData state)
        {
            return StoryActorStageTransformCalculator.UniformWorldScale(state);
        }

        public static Vector2 CalculateActorWorldSize(StoryActorStateData state, Sprite sprite)
        {
            Vector2 sourceSize = ResolveActorSourceSize(sprite);
            Vector3 scale = CalculateActorWorldScale(state);
            return new Vector2(Mathf.Abs(sourceSize.x * scale.x), Mathf.Abs(sourceSize.y * scale.y));
        }

        public static Vector2 CalculateActorPreviewSize(
            StoryActorStateData state,
            Sprite sprite,
            Vector2 cameraPixelSize,
            float cameraWorldWidth = DefaultCameraWorldWidth)
        {
            float pixelsPerWorld = cameraPixelSize.x / Mathf.Max(0.0001f, cameraWorldWidth);
            Vector2 worldSize = CalculateActorWorldSize(state, sprite);
            return new Vector2(
                Mathf.Max(8f, worldSize.x * pixelsPerWorld),
                Mathf.Max(8f, worldSize.y * pixelsPerWorld));
        }

        /// <summary>
        /// Returns the actor pixel rect in the camera panel.
        /// stageLocalPosition (0,0) maps to panel center. (2,0) = 2 world units * pixelsPerWorld right.
        /// Actor pivot is center.
        /// </summary>
        public static Rect CalculateActorPreviewRect(
            StoryActorStateData state,
            Sprite sprite,
            Vector2 cameraPixelSize,
            float cameraWorldWidth = DefaultCameraWorldWidth)
        {
            Vector2 size = CalculateActorPreviewSize(state, sprite, cameraPixelSize, cameraWorldWidth);
            float pixelsPerWorld = cameraPixelSize.x / Mathf.Max(0.0001f, cameraWorldWidth);
            Vector2 localPos = state.stageLocalPosition;

            float panelCenterX = cameraPixelSize.x * 0.5f;
            float panelCenterY = cameraPixelSize.y * 0.5f;
            float screenX = panelCenterX + localPos.x * pixelsPerWorld;
            float screenY = panelCenterY - localPos.y * pixelsPerWorld;

            return new Rect(screenX - size.x * 0.5f, screenY - size.y * 0.5f, size.x, size.y);
        }

        /// <summary>
        /// Returns the background world scale.
        /// finalUniformScale = BackgroundDefinitionSO.BaseScaleMultiplier * state.scaleMultiplier
        /// No cover-scale or overscan. User controls size via scaleMultiplier.
        /// </summary>
        public static Vector3 CalculateBackgroundWorldScale(StoryBackgroundStateData state)
        {
            float baseScale = state?.background != null ? state.background.BaseScaleMultiplier : 1f;
            float lineScale = state != null && state.scaleMultiplier > 0f ? state.scaleMultiplier : 1f;
            float uniform = baseScale * lineScale;
            return new Vector3(uniform, uniform, 1f);
        }

        /// <summary>
        /// Returns the background pixel rect in the camera panel.
        /// parallaxBasePixel is the pixel position of the parallaxBase point.
        /// state.stageLocalPosition is added as world-unit offset.
        /// </summary>
        public static Rect CalculateBackgroundPreviewRect(
            StoryBackgroundStateData state,
            Sprite sprite,
            Vector2 parallaxBasePixel,
            float pixelsPerWorld)
        {
            Vector3 worldScale = CalculateBackgroundWorldScale(state);
            Vector2 sourceSize = sprite != null && sprite.bounds.size.x > 0f
                ? (Vector2)sprite.bounds.size
                : new Vector2(DefaultCameraWorldWidth, DefaultCameraWorldWidth * 9f / 16f);

            float w = Mathf.Max(8f, Mathf.Abs(sourceSize.x * worldScale.x) * pixelsPerWorld);
            float h = Mathf.Max(8f, Mathf.Abs(sourceSize.y * worldScale.y) * pixelsPerWorld);

            Vector2 localPos = state != null ? state.stageLocalPosition : Vector2.zero;
            float cx = parallaxBasePixel.x + localPos.x * pixelsPerWorld;
            float cy = parallaxBasePixel.y - localPos.y * pixelsPerWorld;

            return new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);
        }

        private static Vector2 ResolveActorSourceSize(Sprite sprite)
        {
            if (sprite != null && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
                return sprite.bounds.size;

            return new Vector2(1f / FallbackActorAspectHeightOverWidth, 1f);
        }
    }
}
