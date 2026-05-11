using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets._00._Work.PCM._02._Scripts._TileChange
{
    [CreateAssetMenu(fileName = "TileMapChangeSO", menuName = "SO/TileMapChangeSO")]
    public class TileMapChangeSO : ScriptableObject
    {
        public int id;
        public List<TileBase> tile = new();
    }
}