using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00._Work.CheolYee._02._Scripts.TileMaps
{
    [CreateAssetMenu(fileName = "IsoWallTile", menuName = "Tiles/Iso Wall Tile (Offset)")]
    public class IsoWallTile : TileBase
    {
        [Header("Visual")]
        [SerializeField] private Sprite sprite;
        [SerializeField] private bool useTint;
        [SerializeField] private Color leftWallColor = Color.white;
        [SerializeField] private Color rightWallColor = Color.white;

        [Header("Rule")]
        [Tooltip("X축으로 이어진 타일이 Left Wall인지 Right Wall인지 결정")]
        [SerializeField] private WallSide xAxisWallSide = WallSide.Left;
        [Tooltip("코너(X·Y 둘 다 이웃)일 때 어느 축을 우선할지")]
        [SerializeField] private AxisPriority axisPriority = AxisPriority.XFirst;
        [Tooltip("고립된 1칸 타일의 기본 방향")]
        [SerializeField] private WallSide isolatedWallSide = WallSide.Left;

        [Header("Transform")]
        [SerializeField] private Vector2 defaultPlacementPivotPixels = Vector2.zero;
        [SerializeField] private bool useMirrorPivotCompensation = true;
        [SerializeField] private Vector2 mirroredPlacementPivotPixels = new(256f, 128f);

        [Header("Physics")]
        [SerializeField] private Tile.ColliderType colliderType = Tile.ColliderType.None;

        [Header("Offset")]
        [Tooltip("이 타일 좌표를 기준점(중앙)으로 삼아 오프셋 계산")]
        [SerializeField] private Vector3Int centerTilePosition = Vector3Int.zero;
        [Tooltip("아이소메트릭 그리드 기울기 각도 (기본 26.565°)")]
        [SerializeField] private float isoAngleDegrees = 26.565f;
        [Tooltip("그리드 셀 크기 (X, Y). 기본값: Unity 표준 Iso (1, 0.5)")]
        [SerializeField] private Vector2 gridCellSize = new(1f, 0.5f);
        [Tooltip("스프라이트 PPU (기본 512)")]
        [SerializeField] private float pixelsPerUnit = 512f;
        [Tooltip("Left Wall 타일 간격 (픽셀/그리드 단위). X = 그리드 X축(우하향), Y = 그리드 Y축(좌하향)")]
        [SerializeField] private Vector2 leftWallOffsetPixels = Vector2.zero;
        [Tooltip("Right Wall 타일 간격 (픽셀/그리드 단위). X = 그리드 X축(우하향), Y = 그리드 Y축(좌하향)")]
        [SerializeField] private Vector2 rightWallOffsetPixels = Vector2.zero;

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

            bool hasXNeighbor = HasSameTile(tilemap, position + Vector3Int.left)
                             || HasSameTile(tilemap, position + Vector3Int.right);
            bool hasYNeighbor = HasSameTile(tilemap, position + Vector3Int.up)
                             || HasSameTile(tilemap, position + Vector3Int.down);

            WallSide resolvedSide = ResolveWallSide(hasXNeighbor, hasYNeighbor);
            bool mirrorX = resolvedSide == WallSide.Right;

            tileData.transform = BuildTransform(mirrorX, position);
            tileData.color = useTint
                ? (resolvedSide == WallSide.Left ? leftWallColor : rightWallColor)
                : Color.white;
        }

        private WallSide ResolveWallSide(bool hasXNeighbor, bool hasYNeighbor)
        {
            if (hasXNeighbor && !hasYNeighbor) return xAxisWallSide;
            if (!hasXNeighbor && hasYNeighbor) return Opposite(xAxisWallSide);
            if (hasXNeighbor && hasYNeighbor)
                return axisPriority == AxisPriority.XFirst ? xAxisWallSide : Opposite(xAxisWallSide);
            return isolatedWallSide;
        }

        private WallSide Opposite(WallSide side) =>
            side == WallSide.Left ? WallSide.Right : WallSide.Left;

        private bool HasSameTile(ITilemap tilemap, Vector3Int pos) =>
            tilemap.GetTile(pos) == this;

        private Matrix4x4 BuildTransform(bool mirrorX, Vector3Int position)
        {
            Vector2 offsetPixels = mirrorX ? rightWallOffsetPixels : leftWallOffsetPixels;
            Vector2 extraOffset = ComputeOffsetWorld(position, offsetPixels);

            if (sprite == null)
                return mirrorX
                    ? BuildMirrorMatrix(extraOffset)
                    : BuildPlacementMatrix(extraOffset);

            if (!mirrorX)
                return BuildPlacementMatrix(GetDefaultPlacementWorld(sprite) + extraOffset);

            return useMirrorPivotCompensation
                ? BuildMirrorMatrix(GetMirroredPlacementWorld(sprite) + extraOffset)
                : BuildMirrorMatrix(extraOffset);
        }

        private Vector2 ComputeOffsetWorld(Vector3Int position, Vector2 offsetPixels)
        {
            Vector3Int delta = position - centerTilePosition;
            if (delta.x == 0 && delta.y == 0) return Vector2.zero;

            float ppu = GetSpritePPU(sprite);

            // naturalStep = sqrt((cellX/2)² + (cellY/2)²)
            // For standard iso (angle = atan(cellY/cellX)), this equals cellX/2 / cos(angle),
            // which is the exact per-axis world distance per grid step. Using it as the clamp
            // floor ensures tiles converge to center but never cross it.
            float halfX = gridCellSize.x * 0.5f;
            float halfY = gridCellSize.y * 0.5f;
            float naturalStep = Mathf.Sqrt(halfX * halfX + halfY * halfY);
            float minPx = -naturalStep * ppu;

            float ox = Mathf.Max(offsetPixels.x, minPx) / ppu;
            float oy = Mathf.Max(offsetPixels.y, minPx) / ppu;

            float angle = isoAngleDegrees * Mathf.Deg2Rad;
            float cosA = Mathf.Cos(angle);
            float sinA = Mathf.Sin(angle);

            // Grid X direction in world: (cosA, -sinA)  (right-down in iso view)
            // Grid Y direction in world: (-cosA, -sinA) (left-down in iso view, same screen-Y sign as X)
            // Both grid axes descend in world Y — this is correct for Unity Isometric Z-as-Y
            float worldX = delta.x * ox * cosA  + delta.y * oy * (-cosA);
            float worldY = delta.x * ox * (-sinA) + delta.y * oy * (-sinA);

            return new Vector2(worldX, worldY);
        }

        private static Matrix4x4 BuildPlacementMatrix(Vector2 t) =>
            Matrix4x4.TRS(new Vector3(t.x, t.y, 0f), Quaternion.identity, Vector3.one);

        private static Matrix4x4 BuildMirrorMatrix(Vector2 t) =>
            Matrix4x4.TRS(new Vector3(t.x, t.y, 0f), Quaternion.identity, new Vector3(-1f, 1f, 1f));

        private Vector2 GetDefaultPlacementWorld(Sprite s)
        {
            float ppu = GetSpritePPU(s);
            return new Vector2(
                (s.pivot.x - defaultPlacementPivotPixels.x) / ppu,
                (s.pivot.y - defaultPlacementPivotPixels.y) / ppu);
        }

        private Vector2 GetMirroredPlacementWorld(Sprite s)
        {
            float ppu = GetSpritePPU(s);
            return new Vector2(
                (mirroredPlacementPivotPixels.x - s.pivot.x) / ppu,
                (s.pivot.y - mirroredPlacementPivotPixels.y) / ppu);
        }

        private float GetSpritePPU(Sprite s) =>
            s != null && s.pixelsPerUnit > 0f ? s.pixelsPerUnit
            : pixelsPerUnit > 0f ? pixelsPerUnit
            : 512f;
    }
}
