using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace _00._Work.CheolYee._02._Scripts.TileMaps
{
    public enum WallSide
    {
        Left,
        Right
    }

    public enum AxisPriority
    {
        XFirst,
        YFirst
    }
    
    [CreateAssetMenu(fileName = "IsoAutoWallTile", menuName = "Tiles/Iso Auto Wall Tile")]
    public class IsoAutoWallTile : TileBase
    {
        [Header("Visual")]
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color leftWallColor = Color.white;
        [SerializeField] private Color rightWallColor =  Color.white;
        [SerializeField] private bool useTint = false;
        
        [Header("Rule")]
        [HelpBox("X축으로 이어진 벽이 화면상 Left Wall인지 Right Wall인지 결정")]
        [SerializeField] private WallSide xAxisWallSide = WallSide.Left;
        
        [HelpBox("코너처럼 X/Y 둘 다 연결된 칸에서 어느 축을 우선할지")]
        [SerializeField] private AxisPriority axisPriority = AxisPriority.XFirst;
        
        [HelpBox("혼자 떨어진 벽 1칸일 때 어느 방향으로 볼지")]
        [SerializeField] private WallSide isolatedWallSide = WallSide.Left;

        [Header("Transform")]
        [SerializeField] private Vector2 defaultPlacementPivotPixels = Vector2.zero;
        [SerializeField] private bool useMirrorPivotCompensation = true;
        [FormerlySerializedAs("mirrorPivotCompensationPixels")]
        [SerializeField] private Vector2 mirroredPlacementPivotPixels = new(256f, 128f);

        [Header("Physics")]
        [SerializeField] private Tile.ColliderType colliderType = Tile.ColliderType.None;
        
        private static readonly Matrix4x4 NormalMatrix = Matrix4x4.identity;

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            tilemap.RefreshTile(position);
            tilemap.RefreshTile(position + Vector3Int.left);
            tilemap.RefreshTile(position + Vector3Int.right);
            tilemap.RefreshTile(position + Vector3Int.up);
            tilemap.RefreshTile(position + Vector3Int.down);
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = sprite;
            tileData.gameObject = null;
            tileData.colliderType = colliderType;
            tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;

            bool hasXNeighbor =
                HasSameTile(tilemap, position + Vector3Int.left) ||
                HasSameTile(tilemap, position + Vector3Int.right);

            bool hasYNeighbor =
                HasSameTile(tilemap, position + Vector3Int.up) ||
                HasSameTile(tilemap, position + Vector3Int.down);

            WallSide resolvedSide = ResolveWallSide(hasXNeighbor, hasYNeighbor);

            tileData.transform = BuildTransform(resolvedSide == WallSide.Right);
            tileData.color = useTint
                ? (resolvedSide == WallSide.Left ? leftWallColor : rightWallColor)
                : Color.white;
        }

        private Matrix4x4 BuildTransform(bool mirrorX)
        {
            if (sprite == null)
                return mirrorX ? BuildMirrorMatrix(Vector2.zero) : NormalMatrix;

            if (!mirrorX)
                return BuildPlacementMatrix(GetDefaultPlacementWorld(sprite));

            if (!useMirrorPivotCompensation)
                return BuildMirrorMatrix(Vector2.zero);

            return BuildMirrorMatrix(GetMirroredPlacementWorld(sprite));
        }

        private static Matrix4x4 BuildPlacementMatrix(Vector2 translationWorld)
        {
            return Matrix4x4.TRS(
                new Vector3(translationWorld.x, translationWorld.y, 0f),
                Quaternion.identity,
                Vector3.one);
        }

        private static Matrix4x4 BuildMirrorMatrix(Vector2 compensationWorld)
        {
            return Matrix4x4.TRS(
                new Vector3(compensationWorld.x, compensationWorld.y, 0f),
                Quaternion.identity,
                new Vector3(-1f, 1f, 1f));
        }

        private Vector2 GetDefaultPlacementWorld(Sprite targetSprite)
        {
            float pixelsPerUnit = GetSpritePixelsPerUnitSafe(targetSprite);
            Vector2 actualPivotPixels = targetSprite.pivot;

            float x = (actualPivotPixels.x - defaultPlacementPivotPixels.x) / pixelsPerUnit;
            float y = (actualPivotPixels.y - defaultPlacementPivotPixels.y) / pixelsPerUnit;
            return new Vector2(x, y);
        }

        private Vector2 GetMirroredPlacementWorld(Sprite targetSprite)
        {
            float pixelsPerUnit = GetSpritePixelsPerUnitSafe(targetSprite);
            Vector2 actualPivotPixels = targetSprite.pivot;
            Vector2 desiredPivotPixels = mirroredPlacementPivotPixels;

            float x = (desiredPivotPixels.x - actualPivotPixels.x) / pixelsPerUnit;
            float y = (actualPivotPixels.y - desiredPivotPixels.y) / pixelsPerUnit;
            return new Vector2(x, y);
        }

        private static float GetSpritePixelsPerUnitSafe(Sprite targetSprite)
        {
            return targetSprite != null && targetSprite.pixelsPerUnit > 0f
                ? targetSprite.pixelsPerUnit
                : 100f;
        }

        private WallSide ResolveWallSide(bool hasXNeighbor, bool hasYNeighbor)
        {
            if (hasXNeighbor && !hasYNeighbor)
                return xAxisWallSide;

            if (!hasXNeighbor && hasYNeighbor)
                return Opposite(xAxisWallSide);

            if (hasXNeighbor && hasYNeighbor)
            {
                return axisPriority == AxisPriority.XFirst
                    ? xAxisWallSide
                    : Opposite(xAxisWallSide);
            }

            return isolatedWallSide;
        }

        private WallSide Opposite(WallSide side)
        {
            return side == WallSide.Left ? WallSide.Right : WallSide.Left;
        }

        private bool HasSameTile(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) == this;
        }
    }
}
