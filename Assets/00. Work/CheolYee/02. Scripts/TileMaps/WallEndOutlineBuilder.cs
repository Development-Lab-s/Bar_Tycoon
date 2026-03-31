using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00._Work.CheolYee._02._Scripts.TileMaps
{
    [System.Serializable]
    public class EndpointOutlineSource
    {
        public string label;

        [Header("Tilemaps")]
        public Tilemap source;
        public Tilemap output;
        public Tilemap suppress;

        [Header("Marker")]
        public TileBase markerTile;
        public Color markerColor = new Color(0.42f, 0.30f, 0.26f, 1f);

        [Header("Chain Direction")]
        public Vector3Int negativeDir = Vector3Int.left;
        public Vector3Int positiveDir = Vector3Int.right;

        [Header("Marker Transform (local to source tile transform)")]
        public Vector3 negativeLocalOffset = new Vector3(-0.36f, 0.28f, 0f);
        public Vector3 positiveLocalOffset = new Vector3(0.36f, 0.28f, 0f);

        public float negativeRotationZ = 90f;
        public float positiveRotationZ = 90f;

        public Vector3 markerLocalScale = new Vector3(0.02f, 0.14f, 1f);
    }

    public class WallEndOutlineBuilder : MonoBehaviour
    {
        [SerializeField] private EndpointOutlineSource[] sources;
        [SerializeField] private bool clearOutputsBeforeBuild = true;

        [ContextMenu("Rebuild End Outlines")]
        [Button]
        public void RebuildEndOutlines()
        {
            if (sources == null)
                return;

            foreach (var s in sources)
            {
                if (s == null || s.source == null || s.output == null || s.markerTile == null)
                    continue;

                if (clearOutputsBeforeBuild)
                    s.output.ClearAllTiles();

                BoundsInt bounds = s.source.cellBounds;

                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (!s.source.HasTile(pos))
                        continue;

                    if (s.suppress != null && s.suppress.HasTile(pos))
                        continue;

                    bool hasNegative = s.source.HasTile(pos + s.negativeDir);
                    bool hasPositive = s.source.HasTile(pos + s.positiveDir);

                    // 중간 타일이면 끝점 아님
                    if (hasNegative && hasPositive)
                        continue;

                    // 고립된 단일 타일은 일단 건너뜀
                    if (!hasNegative && !hasPositive)
                        continue;

                    if (!hasNegative)
                    {
                        PlaceMarker(s, pos, s.negativeLocalOffset, s.negativeRotationZ);
                    }
                    else if (!hasPositive)
                    {
                        PlaceMarker(s, pos, s.positiveLocalOffset, s.positiveRotationZ);
                    }
                }
            }
        }

        private void PlaceMarker(EndpointOutlineSource s, Vector3Int cellPos, Vector3 localOffset, float rotationZ)
        {
            s.output.SetTile(cellPos, s.markerTile);
            s.output.SetTileFlags(cellPos, TileFlags.None);

            Matrix4x4 sourceMatrix = s.source.GetTransformMatrix(cellPos);
            Matrix4x4 localMatrix =
                Matrix4x4.TRS(localOffset, Quaternion.Euler(0f, 0f, rotationZ), s.markerLocalScale);

            s.output.SetTransformMatrix(cellPos, sourceMatrix * localMatrix);
            s.output.SetColor(cellPos, s.markerColor);
        }
    }
}