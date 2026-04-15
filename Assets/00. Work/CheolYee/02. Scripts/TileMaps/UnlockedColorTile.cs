using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00._Work.CheolYee._02._Scripts.TileMaps
{
    [CreateAssetMenu(fileName = "UnlockedColorTile", menuName = "Tiles/Unlocked Color Tile")]
    public class UnlockedColorTile : Tile
    {
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);
            tileData.flags = TileFlags.None;
        }
    }
}