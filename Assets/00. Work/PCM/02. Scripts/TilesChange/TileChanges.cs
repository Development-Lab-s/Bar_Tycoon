using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Assets._00._Work.PCM._02._Scripts._TileChange
{
    public class TileChanges : MonoBehaviour
    {
        public TileMapChangeSO tileSO;
        private Tilemap tilemap;
        private TileBase oldTile;
        private ModuleOwner _owner;
        private int id;
        private void Awake()
        {
            tilemap = GetComponent<Tilemap>();
        }
        private void Start()
        {     
            int tileCount = tilemap.GetUsedTilesCount();
            TileBase[] usedTiles = new TileBase[tileCount];
            tilemap.GetUsedTilesNonAlloc(usedTiles);
        }
       
        public void TileSetUp(int id)
        {
            int tileCount = tilemap.GetUsedTilesCount();
            TileBase[] usedTiles = new TileBase[tileCount];
            tilemap.GetUsedTilesNonAlloc(usedTiles);

            if (usedTiles.Length == 0) return;

            TileBase targetOldTile = usedTiles[0];
            TileBase newTile = tileSO.tile[id];

            BoundsInt bounds = tilemap.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (tilemap.GetTile(pos) == targetOldTile)
                {
                    tilemap.SetTile(pos, newTile);
                }
            }
        }
    }
}