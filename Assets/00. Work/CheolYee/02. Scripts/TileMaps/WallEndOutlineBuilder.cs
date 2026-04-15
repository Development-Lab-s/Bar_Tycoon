using System.Collections.Generic;
using Alchemy.Inspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
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
        public Color markerColor = Color.red;

        [Header("Chain Direction")]
        public Vector3Int negativeDir = Vector3Int.left;
        public Vector3Int positiveDir = Vector3Int.right;

        [Header("Marker Transform (local to source tile transform)")]
        public Vector3 negativeLocalOffset = Vector3.zero;
        public Vector3 positiveLocalOffset = Vector3.zero;

        public float negativeRotationZ;
        public float positiveRotationZ;

        public Vector3 markerLocalScale = new Vector3(0.25f, 0.25f, 1f);
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

            var cleared = new HashSet<Tilemap>();

            foreach (var s in sources)
            {
                if (s == null || s.source == null || s.output == null || s.markerTile == null)
                    continue;

#if UNITY_EDITOR
                Undo.RecordObject(s.output, "Rebuild End Outlines");
#endif

                if (clearOutputsBeforeBuild && cleared.Add(s.output))
                    s.output.ClearAllTiles();

                int placedCount = 0;
                BoundsInt bounds = s.source.cellBounds;

                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (!s.source.HasTile(pos))
                        continue;

                    if (s.suppress != null && s.suppress.HasTile(pos))
                        continue;

                    bool hasNegative = s.source.HasTile(pos + s.negativeDir);
                    bool hasPositive = s.source.HasTile(pos + s.positiveDir);

                    if (hasNegative && hasPositive)
                        continue;

                    if (!hasNegative && !hasPositive)
                        continue;

                    if (!hasNegative)
                    {
                        PlaceMarker(s, pos, s.negativeLocalOffset, s.negativeRotationZ);
                        placedCount++;
                    }
                    else
                    {
                        PlaceMarker(s, pos, s.positiveLocalOffset, s.positiveRotationZ);
                        placedCount++;
                    }
                }

                s.output.RefreshAllTiles();

#if UNITY_EDITOR
                EditorUtility.SetDirty(s.output);
                EditorSceneManager.MarkSceneDirty(s.output.gameObject.scene);
#endif

                Debug.Log($"[{s.label}] placed outline markers: {placedCount}");
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