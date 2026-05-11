using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _00._Work.CheolYee._02._Scripts.TileMaps
{
    [System.Serializable]
    public class WallOccupancySource
    {
        public string label = "Wall";
        public Tilemap tilemap;

        [Tooltip("0=R, 1=G, 2=B, 3=A")]
        [Range(0, 3)] public int channel;
    }

    /// <summary>
    /// 벽 타일맵들의 점유 상태를 작은 RGBA 텍스처로 베이크해서
    /// 쉐이더 전역(_WallOccupancyMap 등)으로 공유합니다.
    /// 같은 채널을 두 타일맵이 공유하면 OR로 합쳐집니다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class WallOccupancyBaker : MonoBehaviour
    {
        
        [Header("Sources")]
        [HelpBox("최대 4개. 각 타일맵을 R/G/B/A 채널 하나에 매핑하세요.\n" +
                 "예) 외벽L=R, 외벽R=G, 내벽L=B, 내벽R=A")]
        [SerializeField] private List<WallOccupancySource> sources = new();

        [Header("Settings")]
        [SerializeField, Min(0)] private int padding = 4;
        [SerializeField] private bool autoRebakeOnTilemapChange = true;
        [SerializeField] private bool bakeOnEnable = true;

        [Header("Debug Readouts (Read Only)")]
        [SerializeField, ReadOnly] private Texture2D bakedTexture;
        [SerializeField, ReadOnly] private Vector2Int textureSize;
        [SerializeField, ReadOnly] private Vector3Int boundsMin;
        [SerializeField, ReadOnly] private Vector3Int boundsMax;
        [SerializeField, ReadOnly] private Vector2 worldOrigin;
        [SerializeField, ReadOnly] private Vector2 isoBasisX;
        [SerializeField, ReadOnly] private Vector2 isoBasisY;

        // ── Shader Globals ──
        private static readonly int OccupancyMapID    = Shader.PropertyToID("_WallOccupancyMap");
        private static readonly int OccupancyOriginID = Shader.PropertyToID("_WallOccupancyOrigin");
        private static readonly int OccupancyBasisXid = Shader.PropertyToID("_WallOccupancyBasisX");
        private static readonly int OccupancyBasisYid = Shader.PropertyToID("_WallOccupancyBasisY");
        private static readonly int OccupancySizeID   = Shader.PropertyToID("_WallOccupancyMapSize");

        private bool _isBaking;

        public Texture2D OccupancyTexture => bakedTexture;

        // ─────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────
        private void OnEnable()
        {
            Tilemap.tilemapTileChanged += OnTilemapChanged;
            if (bakeOnEnable) Bake();
        }

        private void OnDisable()
        {
            Tilemap.tilemapTileChanged -= OnTilemapChanged;
        }

        private void OnTilemapChanged(Tilemap t, Tilemap.SyncTile[] _)
        {
            if (!autoRebakeOnTilemapChange) return;
            if (_isBaking) return;
            if (!IsTrackedTilemap(t)) return;
            Bake();
        }

        private bool IsTrackedTilemap(Tilemap t)
        {
            if (sources == null) return false;
            foreach (var tile in sources)
            {
                if (tile != null && tile.tilemap == t) return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────────────
        //  Bake
        // ─────────────────────────────────────────────────────
        [Button]
        private void Bake()
        {
            if (_isBaking) return;
            _isBaking = true;
            try { BakeInternal(); }
            finally { _isBaking = false; }
        }

        private void BakeInternal()
        {
            if (sources == null || sources.Count == 0)
            {
                Debug.LogWarning("[WallOccupancyBaker] Sources가 비어있습니다.");
                return;
            }

            // 1) 참조 Grid 찾기
            Grid refGrid = null;
            foreach (var s in sources)
            {
                if (s != null && s.tilemap != null && s.tilemap.layoutGrid != null)
                {
                    refGrid = s.tilemap.layoutGrid;
                    break;
                }
            }
            if (refGrid == null)
            {
                Debug.LogWarning("[WallOccupancyBaker] 유효한 Tilemap.layoutGrid를 찾을 수 없습니다.");
                return;
            }

            // 2) 합집합 cellBounds 계산
            BoundsInt? unionOpt = null;
            foreach (var s in sources)
            {
                if (s == null || s.tilemap == null) continue;
                s.tilemap.CompressBounds();
                var b = s.tilemap.cellBounds;
                if (b.size.x <= 0 || b.size.y <= 0) continue;

                if (!unionOpt.HasValue)
                {
                    unionOpt = b;
                }
                else
                {
                    var u = unionOpt.Value;
                    Vector3Int mn = Vector3Int.Min(u.min, b.min);
                    Vector3Int mx = Vector3Int.Max(u.max, b.max);
                    u.SetMinMax(mn, mx);
                    unionOpt = u;
                }
            }

            if (!unionOpt.HasValue)
            {
                Debug.LogWarning("[WallOccupancyBaker] 모든 Source 타일맵이 비어있습니다.");
                return;
            }

            var bounds = unionOpt.Value;
            bounds.min -= new Vector3Int(padding, padding, 0);
            bounds.max += new Vector3Int(padding, padding, 0);

            int w = bounds.size.x;
            int h = bounds.size.y;
            if (w <= 0 || h <= 0) return;

            // 3) 텍스처 할당/재사용
            if (bakedTexture == null || bakedTexture.width != w || bakedTexture.height != h)
            {
                if (bakedTexture != null)
                {
                    if (Application.isPlaying) Destroy(bakedTexture);
                    else DestroyImmediate(bakedTexture);
                }
                bakedTexture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "WallOccupancyMap",
                    hideFlags = HideFlags.DontSave
                };
            }

            // 4) 픽셀 채우기 (채널별 OR)
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var cell = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                    byte r = 0, g = 0, bl = 0, a = 0;

                    foreach (var s in sources)
                    {
                        if (s == null || s.tilemap == null) continue;
                        if (!s.tilemap.HasTile(cell)) continue;
                        switch (s.channel)
                        {
                            case 0: r = 255; break;
                            case 1: g = 255; break;
                            case 2: bl = 255; break;
                            case 3: a = 255; break;
                        }
                    }

                    pixels[y * w + x] = new Color32(r, g, bl, a);
                }
            }
            bakedTexture.SetPixels32(pixels);
            bakedTexture.Apply(false, false);

            // 5) iso basis (셀 (0,0)/(1,0)/(0,1)의 월드 좌표 차이)
            Vector3 wO = refGrid.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin, 0));
            Vector3 wX = refGrid.CellToWorld(new Vector3Int(bounds.xMin + 1, bounds.yMin, 0));
            Vector3 wY = refGrid.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin + 1, 0));
            Vector2 bX = wX - wO;
            Vector2 bY = wY - wO;

            // 6) 셰이더 글로벌 세팅
            Shader.SetGlobalTexture(OccupancyMapID, bakedTexture);
            Shader.SetGlobalVector(OccupancyOriginID, new Vector4(wO.x, wO.y, 0f, 0f));
            Shader.SetGlobalVector(OccupancyBasisXid, new Vector4(bX.x, bX.y, 0f, 0f));
            Shader.SetGlobalVector(OccupancyBasisYid, new Vector4(bY.x, bY.y, 0f, 0f));
            Shader.SetGlobalVector(OccupancySizeID,   new Vector4(w, h, 1f / w, 1f / h));

            // 7) 디버그 readout
            textureSize = new Vector2Int(w, h);
            boundsMin   = bounds.min;
            boundsMax   = bounds.max;
            worldOrigin = wO;
            isoBasisX   = bX;
            isoBasisY   = bY;
        }

        // ─────────────────────────────────────────────────────
        //  Editor-only Verification
        // ─────────────────────────────────────────────────────
#if UNITY_EDITOR
        [Button]
        public void SaveTextureToPNG()
        {
            if (bakedTexture == null) Bake();
            if (bakedTexture == null) return;

            string path = EditorUtility.SaveFilePanel(
                "Save Occupancy Map",
                Application.dataPath,
                "WallOccupancy",
                "png");
            if (string.IsNullOrEmpty(path)) return;

            byte[] bytes = bakedTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            Debug.Log($"[WallOccupancyBaker] Saved: {path}");
        }

        [Button]
        public void LogOccupiedCells()
        {
            if (bakedTexture == null)
            {
                Debug.LogWarning("[WallOccupancyBaker] 먼저 Rebake 하세요.");
                return;
            }

            int count = 0;
            foreach (var s in sources)
            {
                if (s == null || s.tilemap == null) continue;
                foreach (var pos in s.tilemap.cellBounds.allPositionsWithin)
                {
                    if (!s.tilemap.HasTile(pos)) continue;

                    Vector3 world = s.tilemap.CellToWorld(pos);
                    int px = pos.x - boundsMin.x;
                    int py = pos.y - boundsMin.y;

                    Color c = (px >= 0 && px < textureSize.x && py >= 0 && py < textureSize.y)
                        ? bakedTexture.GetPixel(px, py)
                        : Color.black;

                    string ch = s.channel switch { 0 => "R", 1 => "G", 2 => "B", 3 => "A", _ => "?" };
                    Debug.Log($"[{s.label}] cell={pos}  world=({world.x:F2},{world.y:F2})  " +
                              $"→ pixel=({px},{py})  ch={ch}  " +
                              $"sampled=({c.r:F1},{c.g:F1},{c.b:F1},{c.a:F1})");

                    count++;
                    if (count >= 20) { Debug.Log("[…] 20개 초과로 중단"); return; }
                }
            }
            if (count == 0) Debug.Log("[WallOccupancyBaker] 점유된 셀이 없습니다.");
        }

        [Button]
        public void RefreshShaderGlobals()
        {
            // 다른 시스템이 글로벌을 덮어썼을 때 다시 세팅
            if (bakedTexture == null) { Bake(); return; }
            Shader.SetGlobalTexture(OccupancyMapID, bakedTexture);
            Shader.SetGlobalVector(OccupancyOriginID, new Vector4(worldOrigin.x, worldOrigin.y, 0f, 0f));
            Shader.SetGlobalVector(OccupancyBasisXid, new Vector4(isoBasisX.x, isoBasisX.y, 0f, 0f));
            Shader.SetGlobalVector(OccupancyBasisYid, new Vector4(isoBasisY.x, isoBasisY.y, 0f, 0f));
            Shader.SetGlobalVector(OccupancySizeID,
                new Vector4(textureSize.x, textureSize.y, 1f / Mathf.Max(1, textureSize.x), 1f / Mathf.Max(1, textureSize.y)));
            Debug.Log("[WallOccupancyBaker] Shader globals refreshed.");
        }

        private void OnDrawGizmosSelected()
        {
            if (textureSize.x <= 0) return;

            Grid refGrid = null;
            foreach (var s in sources)
            {
                if (s != null && s.tilemap != null && s.tilemap.layoutGrid != null)
                {
                    refGrid = s.tilemap.layoutGrid;
                    break;
                }
            }
            if (refGrid == null) return;

            Vector3 c00 = refGrid.CellToWorld(new Vector3Int(boundsMin.x, boundsMin.y, 0));
            Vector3 c10 = refGrid.CellToWorld(new Vector3Int(boundsMax.x, boundsMin.y, 0));
            Vector3 c11 = refGrid.CellToWorld(new Vector3Int(boundsMax.x, boundsMax.y, 0));
            Vector3 c01 = refGrid.CellToWorld(new Vector3Int(boundsMin.x, boundsMax.y, 0));

            Gizmos.color = new Color(0.2f, 1f, 1f, 0.8f);
            Gizmos.DrawLine(c00, c10);
            Gizmos.DrawLine(c10, c11);
            Gizmos.DrawLine(c11, c01);
            Gizmos.DrawLine(c01, c00);

            // basis 벡터 표시 (origin에서 방향)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(c00, c00 + (Vector3)isoBasisX);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(c00, c00 + (Vector3)isoBasisY);
        }
#endif
    }
}