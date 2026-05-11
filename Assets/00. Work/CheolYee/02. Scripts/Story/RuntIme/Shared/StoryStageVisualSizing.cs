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

        public static StoryStageCameraMetrics FromOrthographicCamera(Camera camera, float z = 0f)
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

        public Vector3 ActorPosition(Vector2 normalizedPosition, Vector2 normalizedOffset, float z)
        {
            Vector2 position = normalizedPosition + normalizedOffset;
            Vector3 bottom = Vector3.LerpUnclamped(LeftBottom, RightBottom, position.x);
            bottom += Vector3.up * (position.y * Height);
            bottom.z = z;
            return bottom;
        }

        public Vector3 BackgroundPosition(Vector2 normalizedOffset, float z)
        {
            Vector3 position = Center
                + RightDirection * (normalizedOffset.x * Width)
                + Vector3.up * (normalizedOffset.y * Height);
            position.z = z;
            return position;
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

        public static Vector3 CalculateActorWorldScale(StoryActorStateData state, Sprite sprite)
        {
            Vector2 sourceSize = ResolveActorSourceSize(sprite);
            CharacterDefinitionSO actor = state != null ? state.actor : null;
            float targetHeight = actor != null ? actor.BaseWorldHeight * actor.DefaultScaleMultiplier : 3f;
            float baseScale = targetHeight / Mathf.Max(0.0001f, sourceSize.y);
            Vector2 effectiveScale = state != null ? state.EffectiveScale : Vector2.one;
            if (actor == null || actor.PreserveAspectRatio)
            {
                float uniformScale = ResolveActorUniformScale(effectiveScale);
                float xSign = effectiveScale.x < 0f ? -1f : 1f;
                return new Vector3(baseScale * uniformScale * xSign, baseScale * uniformScale, 1f);
            }

            return new Vector3(baseScale * effectiveScale.x, baseScale * effectiveScale.y, 1f);
        }

        public static Vector2 CalculateActorWorldSize(StoryActorStateData state, Sprite sprite)
        {
            Vector2 sourceSize = ResolveActorSourceSize(sprite);
            Vector3 scale = CalculateActorWorldScale(state, sprite);
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

        public static Rect CalculateActorPreviewRect(
            StoryActorStateData state,
            Sprite sprite,
            Vector2 cameraPixelSize,
            float cameraWorldWidth = DefaultCameraWorldWidth)
        {
            Vector2 size = CalculateActorPreviewSize(state, sprite, cameraPixelSize, cameraWorldWidth);
            Vector2 normPos = state.normalizedPosition + state.EffectiveOffset;
            Vector2 pivot = ClampPivot(state.EffectivePivot);
            float x = normPos.x * cameraPixelSize.x - size.x * pivot.x;
            float y = (1f - normPos.y) * cameraPixelSize.y - size.y * (1f - pivot.y);
            return new Rect(x, y, size.x, size.y);
        }

        public static Vector3 CalculateBackgroundWorldScale(
            StoryBackgroundStateData state,
            Sprite sprite,
            StoryStageCameraMetrics camera)
        {
            Vector2 sourceSize = ResolveBackgroundSourceSize(sprite, camera);
            float cover = Mathf.Max(
                camera.Width / Mathf.Max(0.0001f, sourceSize.x),
                camera.Height / Mathf.Max(0.0001f, sourceSize.y));
            float overscan = state != null && state.background != null
                ? state.background.DefaultCoverOverscan
                : 1.08f;
            Vector2 effectiveScale = state != null ? state.EffectiveScale : Vector2.one;
            bool preserveAspect = state == null || state.background == null || state.background.PreserveAspectRatio;
            if (preserveAspect)
            {
                float uniformScale = ResolveBackgroundUniformScale(effectiveScale);
                return new Vector3(cover * overscan * uniformScale, cover * overscan * uniformScale, 1f);
            }

            return new Vector3(cover * overscan * effectiveScale.x, cover * overscan * effectiveScale.y, 1f);
        }

        public static Rect CalculateBackgroundPreviewRect(
            StoryBackgroundStateData state,
            Sprite sprite,
            Vector2 cameraPixelSize,
            float cameraWorldWidth = DefaultCameraWorldWidth)
        {
            float aspect = cameraPixelSize.x / Mathf.Max(0.0001f, cameraPixelSize.y);
            var camera = new StoryStageCameraMetrics(
                Vector3.zero,
                new Vector3(cameraWorldWidth, 0f, 0f),
                aspect);
            Vector2 sourceSize = ResolveBackgroundSourceSize(sprite, camera);
            Vector3 worldScale = CalculateBackgroundWorldScale(state, sprite, camera);
            float pixelsPerWorld = cameraPixelSize.x / Mathf.Max(0.0001f, cameraWorldWidth);
            Vector2 size = new(
                Mathf.Max(8f, Mathf.Abs(sourceSize.x * worldScale.x) * pixelsPerWorld),
                Mathf.Max(8f, Mathf.Abs(sourceSize.y * worldScale.y) * pixelsPerWorld));
            Vector2 offset = state != null ? state.EffectiveOffset : Vector2.zero;
            Vector2 pivot = ClampPivot(state != null ? state.EffectivePivot : new Vector2(0.5f, 0.5f));
            float anchorX = cameraPixelSize.x * 0.5f + offset.x * cameraPixelSize.x;
            float anchorY = cameraPixelSize.y * 0.5f - offset.y * cameraPixelSize.y;
            return new Rect(anchorX - size.x * pivot.x, anchorY - size.y * pivot.y, size.x, size.y);
        }

        public static Vector3 CalculatePivotLocalOffset(Sprite sprite, Vector2 normalizedPivot)
        {
            if (sprite == null)
                return Vector3.zero;

            Bounds bounds = sprite.bounds;
            Vector2 pivot = ClampPivot(normalizedPivot);
            Vector3 localPivot = new(
                bounds.min.x + bounds.size.x * pivot.x,
                bounds.min.y + bounds.size.y * pivot.y,
                0f);
            return -localPivot;
        }

        private static Vector2 ResolveActorSourceSize(Sprite sprite)
        {
            if (sprite != null && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
                return sprite.bounds.size;

            return new Vector2(1f / FallbackActorAspectHeightOverWidth, 1f);
        }

        private static Vector2 ResolveBackgroundSourceSize(Sprite sprite, StoryStageCameraMetrics camera)
        {
            if (sprite != null && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
                return sprite.bounds.size;

            return new Vector2(camera.Width, camera.Height);
        }

        private static float ResolveActorUniformScale(Vector2 scale)
        {
            float y = Mathf.Abs(scale.y);
            if (y > 0.0001f)
                return y;

            float x = Mathf.Abs(scale.x);
            return x > 0.0001f ? x : 1f;
        }

        private static float ResolveBackgroundUniformScale(Vector2 scale)
        {
            float value = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            return value > 0.0001f ? value : 1f;
        }

        private static Vector2 ClampPivot(Vector2 pivot) =>
            new(Mathf.Clamp01(pivot.x), Mathf.Clamp01(pivot.y));
    }
}
