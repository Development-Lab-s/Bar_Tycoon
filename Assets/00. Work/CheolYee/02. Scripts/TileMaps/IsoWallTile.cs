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
        [Tooltip("Left Wall 타일의 오프셋 (픽셀/그리드 단위). X = 그리드 X축 간격, Y = 그리드 Y축 간격")]
        [SerializeField] private Vector2 leftWallOffsetPixels = Vector2.zero;
        [Tooltip("Right Wall 타일의 오프셋 (픽셀/그리드 단위). X = 그리드 X축 간격, Y = 그리드 Y축 간격")]
        [SerializeField] private Vector2 rightWallOffsetPixels = Vector2.zero;

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
            // 구현 예정 (Task 2)
        }
    }
}
