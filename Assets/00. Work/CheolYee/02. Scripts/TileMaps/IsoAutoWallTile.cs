using Alchemy.Inspector;
using UnityEngine;
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

        [Header("Physics")]
        [SerializeField] private Tile.ColliderType colliderType = Tile.ColliderType.None;
        
        private static readonly Matrix4x4 NormalMatrix = Matrix4x4.identity;
        private static readonly Matrix4x4 FlipXMatrix =
            Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, 1f, 1f));

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

            bool mirrorX = resolvedSide == WallSide.Right;

            tileData.transform = mirrorX ? FlipXMatrix : NormalMatrix;
            tileData.color = useTint
                ? (resolvedSide == WallSide.Left ? leftWallColor : rightWallColor)
                : Color.white;
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
