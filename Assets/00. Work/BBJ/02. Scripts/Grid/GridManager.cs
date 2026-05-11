using BBJ.GridSystem.Objects;
using BBJ.GridSystem.Pathfind;
using UnityEngine;

namespace BBJ.GridSystem
{
    [RequireComponent(typeof(Grid))]
    public class GridManager : MonoBehaviour
    {
        [field: SerializeField] public Vector2Int Size { get; private set; }
        [field: SerializeField] public Vector2Int Offset { get; private set; }

        [SerializeField] private Grid _gridCompo;
        private Node[,] _grid;

        private static readonly Vector2Int[] rhombusMatrix =
        {
            new(0, 0), new(0, 1),
            new(1, 1), new(1, 0)
        };

        private Mesh _mapGridMesh;

#if UNITY_EDITOR
        [Header("Gizmo Settings")]
        [SerializeField] private bool _showWalkabilityMap = true; // 전체 타일 상태 보기 켜기/끄기

        private Mesh _selectGridMesh;
        private Mesh _cellTemplateMesh; // 1칸짜리 도장(Template) 역할을 할 메쉬
        private bool _isSelectGridWalkable = true;
#endif

        private void Awake()
        {
            _gridCompo = GetComponent<Grid>();
            CreateGrid();
        }

        private void CreateGrid()
        {
            if (Size.x <= 0 || Size.y <= 0) return;

            _grid = new Node[Size.x, Size.y];
            for (int x = 0; x < Size.x; x++)
                for (int y = 0; y < Size.y; y++)
                {
                    Vector3 worldPoint = _gridCompo.CellToWorld(
                        new Vector3Int(x, y, 0) + (Vector3Int)Offset);
                    _grid[x, y] = new Node(true, worldPoint, x, y);
                }
        }

        public int MaxSize => Size.x * Size.y;

        public bool TryGetCellToNode(int x, int y, out Node node)
        {
            node = null;
            if (x >= 0 && x < Size.x && y >= 0 && y < Size.y)
                node = _grid[x, y];
            return node != null;
        }

        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            if (_gridCompo == null) return null;
            Vector3Int pos = _gridCompo.WorldToCell(worldPosition) - (Vector3Int)Offset;
            if (TryGetCellToNode(pos.x, pos.y, out Node node))
                return node;

            return null;
        }

        public void ApplyObstacleAt(ObjectData data, Vector2Int cellIndex)
        {
            if (data == null) return;

            if (TryGetCellToNode(cellIndex.x, cellIndex.y, out Node rootNode))
                rootNode.walkable = data.IsWalkable;

            if (data.BlockedOffsets == null) return;
            foreach (var offset in data.BlockedOffsets)
            {
                if (TryGetCellToNode(cellIndex.x + offset.x, cellIndex.y + offset.y, out Node offsetNode))
                    offsetNode.walkable = data.IsWalkable;
            }
        }

        public Vector3 CellToWorld(Vector2Int cellIndex)
        {
            if (_gridCompo == null) return Vector3.zero;
            return _gridCompo.CellToWorld((Vector3Int)(cellIndex + Offset));
        }

#if UNITY_EDITOR
        public void SelectGridGizmoSetting(Vector3 selectWorldPos)
        {
            if (_gridCompo == null || _grid == null) return;

            Vector3Int selectGridPos = _gridCompo.WorldToCell(selectWorldPos * 2);
            selectGridPos /= 2;

            _selectGridMesh = CreateQuadMesh(Vector3Int.one, selectGridPos);

            Vector3Int indexPos = selectGridPos - (Vector3Int)Offset;
            indexPos.x = Mathf.Clamp(indexPos.x, 0, Size.x - 1);
            indexPos.y = Mathf.Clamp(indexPos.y, 0, Size.y - 1);

            if (indexPos.x >= 0 && indexPos.x < Size.x && indexPos.y >= 0 && indexPos.y < Size.y)
            {
                _isSelectGridWalkable = _grid[indexPos.x, indexPos.y].walkable;
            }
        }
#endif

        private void OnValidate()
        {
            if (_gridCompo == null) _gridCompo = GetComponent<Grid>();
            MapGridGizmoSetting();

#if UNITY_EDITOR
            GenerateCellTemplateMesh();
#endif
        }

        private void MapGridGizmoSetting()
        {
            if (_gridCompo == null || Size.x <= 0 || Size.y <= 0) return;
            _mapGridMesh = CreateQuadMesh((Vector3Int)Size, (Vector3Int)Offset);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 개별 타일을 그릴 때 사용할 기준점 (0,0)에 생성된 1칸짜리 메쉬 도장
        /// </summary>
        private void GenerateCellTemplateMesh()
        {
            if (_gridCompo == null) return;

            Vector3[] vertices = new Vector3[rhombusMatrix.Length];
            Vector3 originPos = _gridCompo.CellToWorld(Vector3Int.zero);

            for (int i = 0; i < rhombusMatrix.Length; ++i)
            {
                Vector3Int localCellPos = new Vector3Int(rhombusMatrix[i].x, rhombusMatrix[i].y, 0);
                // 기준 원점(originPos)으로부터의 상대 좌표만 구해서 저장
                vertices[i] = _gridCompo.CellToWorld(localCellPos) - originPos;
            }

            _cellTemplateMesh = new Mesh();
            _cellTemplateMesh.name = "CellTemplateMesh";
            _cellTemplateMesh.vertices = vertices;
            _cellTemplateMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            _cellTemplateMesh.RecalculateNormals();
        }
#endif

        private Mesh CreateQuadMesh(Vector3Int gridSize, Vector3Int offset)
        {
            Vector3[] vertices = new Vector3[rhombusMatrix.Length];

            for (int i = 0; i < rhombusMatrix.Length; ++i)
            {
                Vector3Int localCellPos = new Vector3Int(
                    gridSize.x * rhombusMatrix[i].x,
                    gridSize.y * rhombusMatrix[i].y,
                    0
                );
                vertices[i] = _gridCompo.CellToWorld(localCellPos + offset);
            }

            Mesh mesh = new Mesh();
            mesh.name = "GridQuadMesh";
            mesh.vertices = vertices;
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();

            return mesh;
        }

        private void OnDrawGizmosSelected()
        {
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.identity;

            // 1. 전체 그리드 외곽선
            if (_mapGridMesh != null)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.5f); // 바닥에 아주 옅게 깔아줌
                Gizmos.DrawMesh(_mapGridMesh, Vector3.zero, Quaternion.identity);
                //Gizmos.color = Color.blue;
                //Gizmos.DrawMesh(_mapGridMesh, Vector3.zero, Quaternion.identity);
            }

#if UNITY_EDITOR
            // 2. [추가된 부분] 런타임에 배열을 돌며 각 칸의 Walkable 상태 표시
            if (_showWalkabilityMap && _grid != null && _cellTemplateMesh != null)
            {
                foreach (Node node in _grid)
                {
                    if (node == null) continue;

                    // 걸을 수 있으면 녹색, 없으면 붉은색 (반투명)
                    Color tileColor = node.walkable ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0.7f);
                    Gizmos.color = tileColor;

                    // 각 노드의 월드 좌표(worldPosition) 위치에 도장 찍듯 메쉬를 그림
                    Gizmos.DrawMesh(_cellTemplateMesh, node.worldPosition, Quaternion.identity);

                    // 윤곽선을 살짝 그려주면 타일 구분이 더 명확해짐
                    //Gizmos.color = node.walkable ? new Color(0f, 1f, 0f, 0.8f) : new Color(1f, 0f, 0f, 0.8f);
                    //Gizmos.DrawWireMesh(_cellTemplateMesh, node.worldPosition, Quaternion.identity);
                }
            }

            // 3. 현재 마우스로 선택된(호버링된) 셀 영역 표시
            if (_selectGridMesh != null)
            {
                Color baseColor = _isSelectGridWalkable ? Color.cyan : Color.magenta;

                Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.6f);
                Gizmos.DrawMesh(_selectGridMesh, Vector3.zero, Quaternion.identity);

                Gizmos.color = baseColor;
                Gizmos.DrawWireMesh(_selectGridMesh, Vector3.zero, Quaternion.identity);
            }
#endif
            Gizmos.matrix = originalMatrix;
        }
    }
}