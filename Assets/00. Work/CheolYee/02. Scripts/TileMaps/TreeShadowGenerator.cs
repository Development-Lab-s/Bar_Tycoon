using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00._Work.CheolYee._02._Scripts.TileMaps
{
    /// <summary>
    /// 나무 Tilemap을 읽어 Shadow Tilemap에 그림자 타일을 자동 생성합니다.
    /// </summary>
    public class TreeShadowGenerator : MonoBehaviour
    {
        [Header("Tilemaps")]
        public Tilemap treeTilemap;
        public Tilemap shadowTilemap;

        [Header("Shadow Tile Asset")]
        // 타원형 흰색 스프라이트를 가진 기본 Tile 에셋
        public TileBase shadowTile;

        [Header("Isometric Shadow Offset (world space)")]
        // 아이소메트릭 특성상 오른쪽 아래 방향으로 오프셋
        public Vector3 shadowWorldOffset = new Vector3(0.18f, -0.10f, 0f);

        void Start()
        {
            GenerateShadows();
        }

        public void GenerateShadows()
        {
            if (treeTilemap == null || shadowTilemap == null || shadowTile == null)
            {
                Debug.LogWarning("[TreeShadowGenerator] 필드를 모두 연결해주세요.");
                return;
            }

            // Shadow Tilemap 위치를 오프셋만큼 이동
            // → 같은 셀 좌표를 찍어도 월드 위치가 살짝 밀림
            shadowTilemap.transform.position = shadowWorldOffset;

            // 기존 그림자 초기화
            shadowTilemap.ClearAllTiles();

            // 나무 타일이 존재하는 셀마다 그림자 타일 찍기
            treeTilemap.CompressBounds();
            BoundsInt bounds = treeTilemap.cellBounds;

            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (treeTilemap.HasTile(pos))
                    shadowTilemap.SetTile(pos, shadowTile);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Regenerate Shadows")]
        void RegenerateInEditor() => GenerateShadows();
#endif
    }
}